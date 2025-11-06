using OpenTK;
using OpenTK.Graphics.OpenGL;

using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;
using SPICA.Rendering.Animation;
using SPICA.Rendering.Shaders;
using SPICA.Rendering.SPICA_GL;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SPICA.Rendering
{
    public class Model : IDisposable
    {
        internal Renderer Renderer;
        internal H3DModel BaseModel;
        internal List<Mesh> Meshes0;
        internal List<Mesh> Meshes1;
        internal List<Mesh> Meshes2;
        internal List<Mesh> Meshes3;
        internal List<Shader> Shaders;
        internal Matrix4[] InverseTransforms;
        internal Matrix4[] SkeletonTransforms;
        internal MaterialState[] MaterialStates;
        internal bool[] MeshNodeVisibilities;
        internal bool[] MeshIndexVisibilities;

        public readonly SkeletalAnimation SkeletalAnim;
        public readonly MaterialAnimation MaterialAnim;
        public readonly VisibilityAnimation VisibilityAnim;

        private Dictionary<int, int> ShaderHashes;

        public Matrix4 Transform;

        public string Name
        {
            get { return BaseModel.Name; }
            set { BaseModel.Name = value; }
        }

        public bool Visible
        {
            get { return BaseModel.IsVisible; }
            set { BaseModel.IsVisible = value; }
        }

        public Model(Renderer Renderer, Model cache)
        {
            //Transfer over all the model data to new model instance
            this.Renderer = Renderer;
            this.BaseModel = cache.BaseModel;
            this.Meshes0 = cache.Meshes0;
            this.Meshes1 = cache.Meshes1;
            this.Meshes2 = cache.Meshes2;
            this.Meshes3 = cache.Meshes3;
            this.Shaders = cache.Shaders;
            this.InverseTransforms = cache.InverseTransforms;
            this.SkeletonTransforms = cache.SkeletonTransforms;
            this.ShaderHashes = cache.ShaderHashes;
            this.SkeletalAnim = cache.SkeletalAnim;
            this.MaterialAnim = cache.MaterialAnim;
            this.VisibilityAnim = cache.VisibilityAnim;
            this.Transform = cache.Transform;
            this.MeshNodeVisibilities = cache.MeshNodeVisibilities;
            this.MeshIndexVisibilities = cache.MeshIndexVisibilities;
            this.MaterialStates = cache.MaterialStates;
        }

        public Model(Renderer Renderer, H3DModel BaseModel)
        {
            this.Renderer = Renderer;
            this.BaseModel = BaseModel;

            Meshes0 = new List<Mesh>();
            Meshes1 = new List<Mesh>();
            Meshes2 = new List<Mesh>();
            Meshes3 = new List<Mesh>();
            Shaders = new List<Shader>();

            ShaderHashes = new Dictionary<int, int>();

            InverseTransforms = new Matrix4[BaseModel.Skeleton.Count];

            for (int Bone = 0; Bone < BaseModel.Skeleton.Count; Bone++)
            {
                InverseTransforms[Bone] = BaseModel.Skeleton[Bone].InverseTransform.ToMatrix4();
            }

            UpdateShaders();

            AddMeshes(Meshes0, BaseModel.MeshesLayer0);
            AddMeshes(Meshes1, BaseModel.MeshesLayer1);
            AddMeshes(Meshes2, BaseModel.MeshesLayer2);
            AddMeshes(Meshes3, BaseModel.MeshesLayer3);

            SkeletalAnim = new SkeletalAnimation(BaseModel.Skeleton);
            MaterialAnim = new MaterialAnimation(BaseModel.Materials);
            VisibilityAnim = new VisibilityAnimation(
                BaseModel.MeshNodesTree,
                BaseModel.MeshNodesVisibility);

            Transform = Matrix4.Identity;

            UpdateAnimationTransforms();
        }

        public H3DMaterial GetMaterial(string name)
        {
            if (BaseModel.Materials.Contains(name))
                return BaseModel.Materials[name];

            return null;
        }

        public void SetMeshIndexVis(int id, bool value)
        {
            if (MeshIndexVisibilities.Length > id)
                MeshIndexVisibilities[id] = value;
        }

        public MaterialState GetState(string name)
        {
            for (int i = 0; i < BaseModel.Materials.Count; i++)
            {
                if (BaseModel.Materials[i].Name == name)
                    return MaterialStates[i];
            }
            return null;
        }

        private void AddMeshes(List<Mesh> Dst, List<H3DMesh> Src)
        {
            foreach (H3DMesh Mesh in Src)
            {
                Dst.Add(new Mesh(this, Mesh));
            }
        }

        private void DisposeMeshes(List<Mesh> Meshes)
        {
            foreach (Mesh Mesh in Meshes)
            {
                Mesh.Dispose();
            }
        }

        public void UpdateShaders()
        {
            DisposeShaders();

            foreach (H3DMaterial Material in BaseModel.Materials)
            {
                H3DMaterialParams Params = Material.MaterialParams;

                int Hash = GetMaterialShaderHash(Params);

                bool HasHash = false;

                if (ShaderHashes.TryGetValue(Hash, out int ShaderIndex))
                {
                    HasHash = true;

                    H3DMaterial m = BaseModel.Materials[ShaderIndex];

                    if (CompareMaterials(m.MaterialParams, Params))
                    {
                        Material.FragmentShader = m.FragmentShader;
                        Shaders.Add(Shaders[ShaderIndex]);

                        continue;
                    }
                }

                if (!HasHash)
                {
                    ShaderHashes.Add(Hash, Shaders.Count);
                }

                FragmentShaderGenerator FragShaderGen = new FragmentShaderGenerator(Params);

                int FragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);
                string frag = FragShaderGen.GetFragShader();

                Shader.CompileAndCheck(FragmentShaderHandle, frag);

                VertexShader VtxShader = Renderer.GetShader(Params.ShaderReference);

                Shader Shdr = new Shader(FragmentShaderHandle, VtxShader);

                Shaders.Add(Shdr);

                UpdateShader(Shdr, Params);

                Material.FragmentShader = frag;
                Material.UpdateShaderUniforms += (o, e) =>
                {
                    UpdateShader(Shdr, (o as H3DMaterial).MaterialParams);
                };
                Material.UpdateShaders += (o, e) =>
                {
                    var mat = (o as H3DMaterial).MaterialParams;
                    FragmentShaderGenerator FragShaderGen2 = new FragmentShaderGenerator(mat);
                    Shader.CompileAndCheck(FragmentShaderHandle, FragShaderGen2.GetFragShader());
                };
            }

            UpdateUniforms();
        }

        static void UpdateShader(Shader Shdr, H3DMaterialParams Params)
        {
            GL.UseProgram(Shdr.Handle);

            GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, "Textures[0]"), 0);
            GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, "Textures[1]"), 1);
            GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, "Textures[2]"), 2);
            GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, "TextureCube"), 3);
            GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, "LUTs[0]"), 4);
            GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, "LUTs[1]"), 5);
            GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, "LUTs[2]"), 6);
            GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, "LUTs[3]"), 7);
            GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, "LUTs[4]"), 8);
            GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, "LUTs[5]"), 9);
            GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, "UVTestPattern"), 20);
            GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, "weightRamp1"), 21);
            GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, "weightRamp2"), 22);
            GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, "weightRampType"), 2);


            for (int i = 0; i < 3; i++)
            {
                int j = i * 2;

                GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, $"LUTs[{6 + j}]"), 10 + j);
                GL.Uniform1(GL.GetUniformLocation(Shdr.Handle, $"LUTs[{7 + j}]"), 11 + j);
            }

            //Pokémon uses this
            Vector4 ShaderParam = Vector4.Zero;

            if (Params.MetaData != null)
            {
                foreach (H3DMetaDataValue MD in Params.MetaData)
                {
                    if (MD.Type == H3DMetaDataType.Single)
                    {
                        switch (MD.Name)
                        {
                            case "$ShaderParam0": ShaderParam.W = (float)MD.Values[0]; break;
                            case "$ShaderParam1": ShaderParam.Z = (float)MD.Values[0]; break;
                            case "$ShaderParam2": ShaderParam.Y = (float)MD.Values[0]; break;
                            case "$ShaderParam3": ShaderParam.X = (float)MD.Values[0]; break;
                        }
                    }
                }
            }

            Shdr.SetVtxVector4(85, ShaderParam);

            //Send values from material matching register ids to names.
            foreach (KeyValuePair<uint, System.Numerics.Vector4> KV in Params.VtxShaderUniforms)
            {
                Shdr.SetVtxVector4((int)KV.Key, KV.Value.ToVector4());
            }

            foreach (KeyValuePair<uint, System.Numerics.Vector4> KV in Params.GeoShaderUniforms)
            {
                Shdr.SetGeoVector4((int)KV.Key, KV.Value.ToVector4());
            }

            Vector4 MatAmbient = new Vector4(
                Params.AmbientColor.R / 255f,
                Params.AmbientColor.G / 255f,
                Params.AmbientColor.B / 255F,
                Params.ColorScale);

            Vector4 MatDiffuse = new Vector4(
                Params.DiffuseColor.R / 255f,
                Params.DiffuseColor.G / 255f,
                Params.DiffuseColor.B / 255f,
                1f);

            Vector4 TexCoordMap = new Vector4(
                Params.TextureSources[0],
                Params.TextureSources[1],
                Params.TextureSources[2],
                Params.TextureSources[3]);

            Shdr.SetVtxVector4(DefaultShaderIds.MatAmbi, MatAmbient);
            Shdr.SetVtxVector4(DefaultShaderIds.MatDiff, MatDiffuse);
            Shdr.SetVtxVector4(DefaultShaderIds.TexcMap, TexCoordMap);
        }

        private static int GetMaterialShaderHash(H3DMaterialParams Params)
        {
            FNV1a HashGen = new FNV1a();

            HashGen.Hash(Params.ShaderReference?.GetHashCode() ?? 0);

            HashGen.Hash(Params.AlphaTest.GetHashCode());

            HashGen.Hash(Params.TranslucencyKind.GetHashCode());

            HashGen.Hash(Params.TexCoordConfig.GetHashCode());

            HashGen.Hash(Params.FresnelSelector.GetHashCode());

            HashGen.Hash(Params.BumpMode.GetHashCode());
            HashGen.Hash(Params.BumpTexture.GetHashCode());

            HashGen.Hash(Params.Constant0Assignment.GetHashCode());
            HashGen.Hash(Params.Constant1Assignment.GetHashCode());
            HashGen.Hash(Params.Constant2Assignment.GetHashCode());
            HashGen.Hash(Params.Constant3Assignment.GetHashCode());
            HashGen.Hash(Params.Constant4Assignment.GetHashCode());
            HashGen.Hash(Params.Constant5Assignment.GetHashCode());

            HashGen.Hash(Params.LUTInputAbsolute.Dist0.GetHashCode());
            HashGen.Hash(Params.LUTInputAbsolute.Dist1.GetHashCode());
            HashGen.Hash(Params.LUTInputAbsolute.Fresnel.GetHashCode());
            HashGen.Hash(Params.LUTInputAbsolute.ReflecR.GetHashCode());
            HashGen.Hash(Params.LUTInputAbsolute.ReflecG.GetHashCode());
            HashGen.Hash(Params.LUTInputAbsolute.ReflecB.GetHashCode());

            HashGen.Hash(Params.LUTInputSelection.Dist0.GetHashCode());
            HashGen.Hash(Params.LUTInputSelection.Dist1.GetHashCode());
            HashGen.Hash(Params.LUTInputSelection.Fresnel.GetHashCode());
            HashGen.Hash(Params.LUTInputSelection.ReflecR.GetHashCode());
            HashGen.Hash(Params.LUTInputSelection.ReflecG.GetHashCode());
            HashGen.Hash(Params.LUTInputSelection.ReflecB.GetHashCode());

            HashGen.Hash(Params.LUTInputScale.Dist0.GetHashCode());
            HashGen.Hash(Params.LUTInputScale.Dist1.GetHashCode());
            HashGen.Hash(Params.LUTInputScale.Fresnel.GetHashCode());
            HashGen.Hash(Params.LUTInputScale.ReflecR.GetHashCode());
            HashGen.Hash(Params.LUTInputScale.ReflecG.GetHashCode());
            HashGen.Hash(Params.LUTInputScale.ReflecB.GetHashCode());

            HashGen.Hash(Params.LUTDist0TableName?.GetHashCode() ?? 0);
            HashGen.Hash(Params.LUTDist1TableName?.GetHashCode() ?? 0);
            HashGen.Hash(Params.LUTFresnelTableName?.GetHashCode() ?? 0);
            HashGen.Hash(Params.LUTReflecRTableName?.GetHashCode() ?? 0);
            HashGen.Hash(Params.LUTReflecGTableName?.GetHashCode() ?? 0);
            HashGen.Hash(Params.LUTReflecBTableName?.GetHashCode() ?? 0);

            HashGen.Hash(Params.LUTDist0SamplerName?.GetHashCode() ?? 0);
            HashGen.Hash(Params.LUTDist1SamplerName?.GetHashCode() ?? 0);
            HashGen.Hash(Params.LUTFresnelSamplerName?.GetHashCode() ?? 0);
            HashGen.Hash(Params.LUTReflecRSamplerName?.GetHashCode() ?? 0);
            HashGen.Hash(Params.LUTReflecGSamplerName?.GetHashCode() ?? 0);
            HashGen.Hash(Params.LUTReflecBSamplerName?.GetHashCode() ?? 0);

            foreach (PICATexEnvStage Stage in Params.TexEnvStages)
            {
                HashGen.Hash(Stage.Source.Color[0].GetHashCode());
                HashGen.Hash(Stage.Source.Color[1].GetHashCode());
                HashGen.Hash(Stage.Source.Color[2].GetHashCode());
                HashGen.Hash(Stage.Source.Alpha[0].GetHashCode());
                HashGen.Hash(Stage.Source.Alpha[1].GetHashCode());
                HashGen.Hash(Stage.Source.Alpha[2].GetHashCode());

                HashGen.Hash(Stage.Operand.Color[0].GetHashCode());
                HashGen.Hash(Stage.Operand.Color[1].GetHashCode());
                HashGen.Hash(Stage.Operand.Color[2].GetHashCode());
                HashGen.Hash(Stage.Operand.Alpha[0].GetHashCode());
                HashGen.Hash(Stage.Operand.Alpha[1].GetHashCode());
                HashGen.Hash(Stage.Operand.Alpha[2].GetHashCode());

                HashGen.Hash(Stage.Combiner.Color.GetHashCode());
                HashGen.Hash(Stage.Combiner.Alpha.GetHashCode());

                HashGen.Hash(Stage.Scale.Color.GetHashCode());
                HashGen.Hash(Stage.Scale.Alpha.GetHashCode());

                HashGen.Hash(Stage.UpdateColorBuffer.GetHashCode());
                HashGen.Hash(Stage.UpdateAlphaBuffer.GetHashCode());
            }

            HashGen.Hash(Params.TexEnvBufferColor.R.GetHashCode());
            HashGen.Hash(Params.TexEnvBufferColor.G.GetHashCode());
            HashGen.Hash(Params.TexEnvBufferColor.B.GetHashCode());
            HashGen.Hash(Params.TexEnvBufferColor.A.GetHashCode());

            return (int)HashGen.HashCode;
        }

        private static bool CompareMaterials(H3DMaterialParams LHS, H3DMaterialParams RHS)
        {
            bool Equals = true;

            Equals &= LHS.AlphaTest.Enabled == RHS.AlphaTest.Enabled;
            Equals &= LHS.AlphaTest.Function == RHS.AlphaTest.Function;

            Equals &= LHS.ShaderReference == RHS.ShaderReference;

            Equals &= LHS.TranslucencyKind == RHS.TranslucencyKind;

            Equals &= LHS.TexCoordConfig == RHS.TexCoordConfig;

            Equals &= LHS.FresnelSelector == RHS.FresnelSelector;

            Equals &= LHS.BumpMode == RHS.BumpMode;
            Equals &= LHS.BumpTexture == RHS.BumpTexture;

            Equals &= LHS.Constant0Assignment == RHS.Constant0Assignment;
            Equals &= LHS.Constant1Assignment == RHS.Constant1Assignment;
            Equals &= LHS.Constant2Assignment == RHS.Constant2Assignment;
            Equals &= LHS.Constant3Assignment == RHS.Constant3Assignment;
            Equals &= LHS.Constant4Assignment == RHS.Constant4Assignment;
            Equals &= LHS.Constant5Assignment == RHS.Constant5Assignment;

            Equals &= LHS.LUTInputAbsolute.Dist0 == RHS.LUTInputAbsolute.Dist0;
            Equals &= LHS.LUTInputAbsolute.Dist1 == RHS.LUTInputAbsolute.Dist1;
            Equals &= LHS.LUTInputAbsolute.Fresnel == RHS.LUTInputAbsolute.Fresnel;
            Equals &= LHS.LUTInputAbsolute.ReflecR == RHS.LUTInputAbsolute.ReflecR;
            Equals &= LHS.LUTInputAbsolute.ReflecG == RHS.LUTInputAbsolute.ReflecG;
            Equals &= LHS.LUTInputAbsolute.ReflecB == RHS.LUTInputAbsolute.ReflecB;

            Equals &= LHS.LUTInputSelection.Dist0 == RHS.LUTInputSelection.Dist0;
            Equals &= LHS.LUTInputSelection.Dist1 == RHS.LUTInputSelection.Dist1;
            Equals &= LHS.LUTInputSelection.Fresnel == RHS.LUTInputSelection.Fresnel;
            Equals &= LHS.LUTInputSelection.ReflecR == RHS.LUTInputSelection.ReflecR;
            Equals &= LHS.LUTInputSelection.ReflecG == RHS.LUTInputSelection.ReflecG;
            Equals &= LHS.LUTInputSelection.ReflecB == RHS.LUTInputSelection.ReflecB;

            Equals &= LHS.LUTInputScale.Dist0 == RHS.LUTInputScale.Dist0;
            Equals &= LHS.LUTInputScale.Dist1 == RHS.LUTInputScale.Dist1;
            Equals &= LHS.LUTInputScale.Fresnel == RHS.LUTInputScale.Fresnel;
            Equals &= LHS.LUTInputScale.ReflecR == RHS.LUTInputScale.ReflecR;
            Equals &= LHS.LUTInputScale.ReflecG == RHS.LUTInputScale.ReflecG;
            Equals &= LHS.LUTInputScale.ReflecB == RHS.LUTInputScale.ReflecB;

            Equals &= LHS.LUTDist0TableName == RHS.LUTDist0TableName;
            Equals &= LHS.LUTDist1TableName == RHS.LUTDist1TableName;
            Equals &= LHS.LUTFresnelTableName == RHS.LUTFresnelTableName;
            Equals &= LHS.LUTReflecRTableName == RHS.LUTReflecRTableName;
            Equals &= LHS.LUTReflecGTableName == RHS.LUTReflecGTableName;
            Equals &= LHS.LUTReflecBTableName == RHS.LUTReflecBTableName;

            Equals &= LHS.LUTDist0SamplerName == RHS.LUTDist0SamplerName;
            Equals &= LHS.LUTDist1SamplerName == RHS.LUTDist1SamplerName;
            Equals &= LHS.LUTFresnelSamplerName == RHS.LUTFresnelSamplerName;
            Equals &= LHS.LUTReflecRSamplerName == RHS.LUTReflecRSamplerName;
            Equals &= LHS.LUTReflecGSamplerName == RHS.LUTReflecGSamplerName;
            Equals &= LHS.LUTReflecBSamplerName == RHS.LUTReflecBSamplerName;

            for (int i = 0; i < 6; i++)
            {
                Equals &= LHS.TexEnvStages[i].Source.Color[0] == RHS.TexEnvStages[i].Source.Color[0];
                Equals &= LHS.TexEnvStages[i].Source.Color[1] == RHS.TexEnvStages[i].Source.Color[1];
                Equals &= LHS.TexEnvStages[i].Source.Color[2] == RHS.TexEnvStages[i].Source.Color[2];
                Equals &= LHS.TexEnvStages[i].Source.Alpha[0] == RHS.TexEnvStages[i].Source.Alpha[0];
                Equals &= LHS.TexEnvStages[i].Source.Alpha[1] == RHS.TexEnvStages[i].Source.Alpha[1];
                Equals &= LHS.TexEnvStages[i].Source.Alpha[2] == RHS.TexEnvStages[i].Source.Alpha[2];

                Equals &= LHS.TexEnvStages[i].Operand.Color[0] == RHS.TexEnvStages[i].Operand.Color[0];
                Equals &= LHS.TexEnvStages[i].Operand.Color[1] == RHS.TexEnvStages[i].Operand.Color[1];
                Equals &= LHS.TexEnvStages[i].Operand.Color[2] == RHS.TexEnvStages[i].Operand.Color[2];
                Equals &= LHS.TexEnvStages[i].Operand.Alpha[0] == RHS.TexEnvStages[i].Operand.Alpha[0];
                Equals &= LHS.TexEnvStages[i].Operand.Alpha[1] == RHS.TexEnvStages[i].Operand.Alpha[1];
                Equals &= LHS.TexEnvStages[i].Operand.Alpha[2] == RHS.TexEnvStages[i].Operand.Alpha[2];

                Equals &= LHS.TexEnvStages[i].Combiner.Color == RHS.TexEnvStages[i].Combiner.Color;
                Equals &= LHS.TexEnvStages[i].Combiner.Alpha == RHS.TexEnvStages[i].Combiner.Alpha;

                Equals &= LHS.TexEnvStages[i].Scale.Color == RHS.TexEnvStages[i].Scale.Color;
                Equals &= LHS.TexEnvStages[i].Scale.Alpha == RHS.TexEnvStages[i].Scale.Alpha;

                Equals &= LHS.TexEnvStages[i].UpdateColorBuffer == RHS.TexEnvStages[i].UpdateColorBuffer;
                Equals &= LHS.TexEnvStages[i].UpdateAlphaBuffer == RHS.TexEnvStages[i].UpdateAlphaBuffer;
            }

            Equals &= LHS.TexEnvBufferColor.R == RHS.TexEnvBufferColor.R;
            Equals &= LHS.TexEnvBufferColor.G == RHS.TexEnvBufferColor.G;
            Equals &= LHS.TexEnvBufferColor.B == RHS.TexEnvBufferColor.B;
            Equals &= LHS.TexEnvBufferColor.A == RHS.TexEnvBufferColor.A;

            return Equals;
        }

        public void UpdateUniforms()
        {
            foreach (Shader Shader in Shaders)
            {
                GL.UseProgram(Shader.Handle);

                int fi = 0;

                for (int i = 0; i < Renderer.Lights.Count; i++)
                {
                    if (!Renderer.Lights[i].Enabled) continue;

                    switch (Renderer.Lights[i].Type)
                    {
                        case LightType.PerFragment:
                            if (fi < 3)
                            {
                                SetFragmentLight(Shader, Renderer.Lights[i], fi++);
                            }
                            break;
                    }
                }

                int LightsCountLocation = GL.GetUniformLocation(Shader.Handle, "LightsCount");

                GL.Uniform1(LightsCountLocation, fi);
            }
        }

        private void SetFragmentLight(Shader Shader, Light Light, int fi)
        {
            int PositionLocation = GL.GetUniformLocation(Shader.Handle, $"Lights[{fi}].Position");
            int DirectionLocation = GL.GetUniformLocation(Shader.Handle, $"Lights[{fi}].Direction");
            int AmbientLocation = GL.GetUniformLocation(Shader.Handle, $"Lights[{fi}].Ambient");
            int DiffuseLocation = GL.GetUniformLocation(Shader.Handle, $"Lights[{fi}].Diffuse");
            int Specular0Location = GL.GetUniformLocation(Shader.Handle, $"Lights[{fi}].Specular0");
            int Specular1Location = GL.GetUniformLocation(Shader.Handle, $"Lights[{fi}].Specular1");
            int LUTInputLocation = GL.GetUniformLocation(Shader.Handle, $"Lights[{fi}].AngleLUTInput");
            int LUTScaleLocation = GL.GetUniformLocation(Shader.Handle, $"Lights[{fi}].AngleLUTScale");
            int AttScaleLocation = GL.GetUniformLocation(Shader.Handle, $"Lights[{fi}].AttScale");
            int AttBiasLocation = GL.GetUniformLocation(Shader.Handle, $"Lights[{fi}].AttBias");
            int DistAttEnbLocation = GL.GetUniformLocation(Shader.Handle, $"Lights[{fi}].DistAttEnb");
            int TwoSidedDiffLocation = GL.GetUniformLocation(Shader.Handle, $"Lights[{fi}].TwoSidedDiff");
            int DirectionalLocation = GL.GetUniformLocation(Shader.Handle, $"Lights[{fi}].Directional");

            GL.Uniform3(PositionLocation, 
                Light.Position.X,
                Light.Position.Y,
                Light.Position.Z);
            GL.Uniform3(DirectionLocation,
                Light.Direction.X,
                Light.Direction.Y,
                Light.Direction.Z);
            GL.Uniform4(AmbientLocation,
                Light.Ambient.X,
                Light.Ambient.Y,
                Light.Ambient.Z,
                Light.Ambient.W);
            GL.Uniform4(DiffuseLocation, 
                Light.Diffuse.X, 
                Light.Diffuse.Y, 
                Light.Diffuse.Z, 
                Light.Diffuse.W);
            GL.Uniform4(Specular0Location,
                Light.Specular0.X,
                Light.Specular0.Y, 
                Light.Specular0.Z, 
                Light.Specular0.W);
            GL.Uniform4(Specular1Location, 
                Light.Specular1.X,
                Light.Specular1.Y,
                Light.Specular1.Z,
                Light.Specular1.W);
            GL.Uniform1(LUTInputLocation, Light.AngleLUTInput);
            GL.Uniform1(LUTScaleLocation, Light.AngleLUTScale);
            GL.Uniform1(AttScaleLocation, Light.AttenuationScale);
            GL.Uniform1(AttBiasLocation, Light.AttenuationBias);
            GL.Uniform1(DistAttEnbLocation, Light.DistAttEnabled ? 1 : 0);
            GL.Uniform1(TwoSidedDiffLocation, Light.TwoSidedDiffuse ? 1 : 0);
            GL.Uniform1(DirectionalLocation, Light.Directional ? 1 : 0);

            Renderer.TryBindLUT(10 + fi * 2,
                Light.AngleLUTTableName,
                Light.AngleLUTSamplerName);

            Renderer.TryBindLUT(11 + fi * 2,
                Light.DistanceLUTTableName,
                Light.DistanceLUTSamplerName);
        }

        public BoundingBox GetModelAABB()
        {
            bool IsFirst = true;

            Vector4 Min = Vector4.Zero;
            Vector4 Max = Vector4.Zero;

            foreach (H3DMesh Mesh in BaseModel.Meshes)
            {
                PICAVertex[] Vertices = Mesh.GetVertices();

                if (Vertices.Length == 0) continue;

                if (IsFirst)
                {
                    Min = Max = Vertices[0].Position.ToVector4();

                    IsFirst = false;
                }

                foreach (PICAVertex Vertex in Vertices)
                {
                    Vector4 P = Vertex.Position.ToVector4();

                    Min = Vector4.Min(Min, P);
                    Max = Vector4.Max(Max, P);
                }
            }

            return new BoundingBox(
                ((Max + Min) * 0.5f).Xyz,
                 (Max - Min).Xyz);
        }

        public void UpdateAnimationTransforms()
        {
            if (BaseModel.Meshes.Count > 0)
            {
                SkeletonTransforms = SkeletalAnim.GetSkeletonTransforms();

                MaterialStates = MaterialAnim.GetMaterialStates();

                MeshNodeVisibilities = VisibilityAnim.GetMeshVisibilities();
                MeshIndexVisibilities = new bool[BaseModel.Meshes.Count];
                for (int i = 0; i < BaseModel.Meshes.Count; i++)
                    MeshIndexVisibilities[i] = true;
            }
        }

        public void RenderMeshesPicking(Vector4 pickingColor)
        {
            RenderMeshesPicking(Meshes0, pickingColor);
            RenderMeshesPicking(Meshes1, pickingColor);
            RenderMeshesPicking(Meshes2, pickingColor);
            RenderMeshesPicking(Meshes3, pickingColor);
        }

        public void RenderLayer1(bool isRenderSelected) => RenderMeshes(Meshes0, isRenderSelected);
        public void RenderLayer2(bool isRenderSelected) => RenderMeshes(Meshes1, isRenderSelected);
        public void RenderLayer3(bool isRenderSelected) => RenderMeshes(Meshes2, isRenderSelected);
        public void RenderLayer4(bool isRenderSelected) => RenderMeshes(Meshes3, isRenderSelected);


        private void RenderMeshesPicking(IEnumerable<Mesh> Meshes, Vector4 pickingColor)
        {
            if (!BaseModel.IsVisible)
                return;

            var transform = Transform * this.BaseModel.WorldTransform.ToMatrix4();

            foreach (Mesh Mesh in Meshes)
            {
                int n = Mesh.BaseMesh.NodeIndex;

                if (n < MeshNodeVisibilities.Length && !MeshNodeVisibilities[n])
                {
                    continue;
                }

                if (Mesh.Index < MeshIndexVisibilities.Length && !MeshIndexVisibilities[Mesh.Index])
                {
                    continue;
                }

                if (!Mesh.BaseMesh.IsVisible)
                    continue;


                Shader Shader = Shaders[Mesh.BaseMesh.MaterialIndex];

                GL.UseProgram(Shader.Handle);

                var normalMatrix = Matrix4.Identity * Renderer.Camera.ViewMatrix;
                normalMatrix.Invert();
                normalMatrix.Transpose();

                Shader.SetVtx4x4Array(DefaultShaderIds.ProjMtx, Renderer.Camera.ProjectionMatrix);
                Shader.SetVtx3x4Array(DefaultShaderIds.ViewMtx, transform * Renderer.Camera.ViewMatrix);
                Shader.SetVtx3x4Array(DefaultShaderIds.NormMtx, normalMatrix * transform.ClearScale());
                Shader.SetVtx3x4Array(DefaultShaderIds.WrldMtx, Matrix4.Identity);

                GL.Uniform1(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.PickingMode), 1);
                GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.PickingColor), pickingColor);

                Mesh.Render(true);
            }
        }

        private void RenderMeshes(IEnumerable<Mesh> Meshes, bool isRenderSelected)
        {
            if (!BaseModel.IsVisible)
                return;

            var transform = Transform * this.BaseModel.WorldTransform.ToMatrix4();

            List<Mesh> PendingMeshes = Meshes.ToList();

            for (int CurrentPriority = 0; CurrentPriority < 256 && PendingMeshes.Count != 0; CurrentPriority++)
            {
                PendingMeshes.RemoveAll(Mesh =>
                {
                    if (Mesh.BaseMesh.Priority != CurrentPriority)
                    {
                        return false;
                    }

                    int n = Mesh.BaseMesh.NodeIndex;

                    if (n < MeshNodeVisibilities.Length && !MeshNodeVisibilities[n])
                    {
                        return true;
                    }

                    if (Mesh.Index < MeshIndexVisibilities.Length && !MeshIndexVisibilities[Mesh.Index])
                    {
                        return true;
                    }


                    if (!Mesh.BaseMesh.IsVisible)
                        return true;

                    Shader Shader = Shaders[Mesh.BaseMesh.MaterialIndex];

                    GL.UseProgram(Shader.Handle);

                    var normalMatrix = Renderer.Camera.ViewMatrix;
                    normalMatrix.Invert();
                    normalMatrix.Transpose();

                    GL.Uniform1(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.PickingMode), 0);
                    GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.PickingColor), Vector4.Zero);

                    Shader.SetVtx4x4Array(DefaultShaderIds.ProjMtx, Renderer.Camera.ProjectionMatrix);
                    Shader.SetVtx3x4Array(DefaultShaderIds.ViewMtx, Renderer.Camera.ViewMatrix);
                    Shader.SetVtx3x4Array(DefaultShaderIds.NormMtx, normalMatrix);
                    Shader.SetVtx3x4Array(DefaultShaderIds.WrldMtx, transform);

                    GL.Uniform1(GL.GetUniformLocation(Shader.Handle, "DisableVertexColor"), 0);
                    int MaterialIndex = Mesh.BaseMesh.MaterialIndex;

                    H3DMaterialParams MP = BaseModel.Materials[MaterialIndex].MaterialParams;

                    MaterialState MS = MaterialStates[Mesh.BaseMesh.MaterialIndex];

                    //For updating the UI, apply the current material data to the state unless it is animating
                    if (!MS.IsAnimated)
                        MS.Reset(BaseModel.Materials[MaterialIndex]);

                    Vector4 MatAmbient = new Vector4(
                        MS.Ambient.R,
                        MS.Ambient.G,
                        MS.Ambient.B,
                        MP.ColorScale);

                    Vector4 MatDiffuse = new Vector4(
                        MS.Diffuse.R,
                        MS.Diffuse.G,
                        MS.Diffuse.B,
                        MS.Diffuse.A);

                    Vector4 MatSelect = new Vector4(
                           MP.SelectionColor.X,
                           MP.SelectionColor.Y,
                           MP.SelectionColor.Z,
                           MP.SelectionColor.W);


                    Shader.SetVtxVector4(DefaultShaderIds.TexTran, new Vector4(
                        MS.Transforms[0].Row3.X,
                        MS.Transforms[0].Row3.Y,
                        MS.Transforms[1].Row3.X,
                        MS.Transforms[1].Row3.Y));

                    for (int i = 0; i < 3; i++)
                    {
                        //Apply certain matrices based on type. Note, env sphere camera uses normal matrix
                        if (MP.TextureCoords[i].MappingType == H3DTextureMappingType.ProjectionMap)
                            Shader.SetVtx3x4Array(DefaultShaderIds.TexMtx0 + (3 * i), Renderer.Camera.ViewMatrix * MS.Transforms[i]);
                        else
                            Shader.SetVtx3x4Array(DefaultShaderIds.TexMtx0 + (3 * i), MS.Transforms[i]);
                    }

                    Shader.SetVtxVector4(DefaultShaderIds.MatAmbi, MatAmbient);
                    Shader.SetVtxVector4(DefaultShaderIds.MatDiff, MatDiffuse);

                    GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.EmissionUniform), MS.Emission);
                    GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.AmbientUniform), MS.Ambient);
                    GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.DiffuseUniform), MS.Diffuse);
                    GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.Specular0Uniform), MS.Specular0);
                    GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.Specular1Uniform), MS.Specular1);
                    GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.Constant0Uniform), MS.Constant0);
                    GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.Constant1Uniform), MS.Constant1);
                    GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.Constant2Uniform), MS.Constant2);
                    GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.Constant3Uniform), MS.Constant3);
                    GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.Constant4Uniform), MS.Constant4);
                    GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.Constant5Uniform), MS.Constant5);
                    GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.CombBufferUniform), MP.TexEnvBufferColor.ToColor4());

                    GL.Uniform1(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.AlphaRefUniform), MP.AlphaTest.Reference / 255.0f);
                    GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.SelectionUniform), MatSelect);

                    GL.Uniform1(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.DebugModeUniform), Renderer.DebugShadingMode);
                    GL.Uniform1(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.DebugLUTModeUniform), Renderer.DebugLUTShadingMode);

                    Shader.SetVtxVector4(DefaultShaderIds.HsLGCol, new Vector4(Renderer.GlobalHsLGCol.X, Renderer.GlobalHsLGCol.Y, Renderer.GlobalHsLGCol.Z, 0.00f));
                    Shader.SetVtxVector4(DefaultShaderIds.HsLSCol, new Vector4(Renderer.GlobalHsLSCol.X, Renderer.GlobalHsLSCol.Y, Renderer.GlobalHsLSCol.Z, 0.00f));
                    Shader.SetVtxVector4(DefaultShaderIds.HsLSDir, new Vector4(0.0f, 0.95703f, 0.28998f, 0.40f));

                    bool isSelected = isRenderSelected || Mesh.BaseMesh.IsSelected || MatSelect.W != 0;

                    Mesh.Texture0Name = MS.Texture0Name;
                    Mesh.Texture1Name = MS.Texture1Name;
                    Mesh.Texture2Name = MS.Texture2Name;

                    if (isSelected)
                    {
                        GL.Enable(EnableCap.StencilTest);
                        GL.Clear(ClearBufferMask.StencilBufferBit);
                        GL.ClearStencil(0);
                        GL.StencilFunc(StencilFunction.Always, 0x1, 0x1);
                        GL.StencilOp(StencilOp.Keep, StencilOp.Replace, StencilOp.Replace);
                    }

                    Mesh.Render();

                    if (isSelected)
                    {
                        GL.Disable(EnableCap.Blend);

                        if (MatSelect.W != 0) // Material select, else mesh select. Material wireframe less visible to view material better
                            GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.SelectionUniform), new Vector4(1, 1, 1, 0.3f));
                        else
                            GL.Uniform4(GL.GetUniformLocation(Shader.Handle, FragmentShaderGenerator.SelectionUniform), new Vector4(1, 1, 1, 1));

                        GL.LineWidth(2);
                        GL.StencilFunc(StencilFunction.Equal, 0x0, 0x1);
                        GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                        Mesh.Render();
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                        GL.Disable(EnableCap.StencilTest);
                        GL.LineWidth(1);
                    }

                    return true;
                });
            }
        }

        private bool Disposed;

        protected virtual void Dispose(bool Disposing)
        {
            if (!Disposed)
            {
                DisposeMeshes(Meshes0);
                DisposeMeshes(Meshes1);
                DisposeMeshes(Meshes2);
                DisposeMeshes(Meshes3);

                DisposeShaders();

                Disposed = true;
            }
        }

        private void DisposeShaders()
        {
            foreach (Shader Shader in Shaders)
            {
                Shader.DetachAllShaders();
                Shader.DeleteFragmentShader();
                Shader.DeleteProgram();
            }

            Shaders.Clear();

            ShaderHashes.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
