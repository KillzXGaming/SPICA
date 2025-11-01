using SPICA.Math3D;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using SPICA.PICA.Commands;
using Newtonsoft.Json;
using SPICA.Formats.CtrH3D.Model.Material;
using System;

namespace SPICA.Formats.CtrGfx.Model.Material
{
    [TypeChoice(0x08000000u, typeof(GfxMaterial))]
    public class GfxMaterial : GfxObject, ICustomSerialization
    {
        [Ignore]
        [JsonIgnore]
        public H3DMaterial H3DMaterial;

        public GfxMaterialFlags Flags;

        public GfxTexCoordConfig   TexCoordConfig;
        public int RenderLayer;

        public GfxMaterialColor Colors;

        public GfxRasterization Rasterization;

        public GfxFragOp FragmentOperation;

        public int UsedTextureCoordsCount;

        [Inline, FixedLength(3)] public GfxTextureCoord[]  TextureCoords;
        [Inline, FixedLength(3)] public GfxTextureMapper[] TextureMappers;

        public GfxProcTextureMapper ProceduralTextureMapper;

        public GfxShaderReference Shader;
        public GfxFragShader      FragmentShader;

        public int ShaderProgramDescIndex;

        public List<GfxShaderParam> ShaderParameters;

        public int LightSetIndex;
        public int FogIndex;

        private uint MaterialFlagsHash;
        private uint ShaderParamsHash;
        private uint TextureCoordsHash;
        private uint TextureSamplersHash;
        private uint TextureMappersHash;
        private uint MaterialColorsHash;
        private uint RasterizationHash;
        private uint FragLightHash;
        private uint FragLightLUTHash;
        private uint FragLightLUTSampHash;
        private uint TextureEnvironmentHash;
        private uint AlphaTestHash;
        private uint FragOpHash;
        private uint UniqueId;

        [JsonIgnore]
        public RenderPreset RenderingPreset
        {
            get
            {
                if (this.FragmentShader.AlphaTest.Test.Enabled)
                    return RenderPreset.Transparent;
                else if (this.FragmentOperation.Blend.Mode == GfxFragOpBlendMode.Blend &&
                         this.FragmentOperation.Blend.Function.ColorSrcFunc == PICABlendFunc.SourceAlpha &&
                         this.FragmentOperation.Blend.Function.ColorDstFunc == PICABlendFunc.OneMinusSourceAlpha &&
                         this.FragmentOperation.Blend.Function.AlphaSrcFunc == PICABlendFunc.SourceAlpha &&
                         this.FragmentOperation.Blend.Function.AlphaDstFunc == PICABlendFunc.OneMinusSourceAlpha)
                    return RenderPreset.Translucent;
                else if (this.FragmentOperation.Blend.Mode == GfxFragOpBlendMode.BlendSeparate)
                    return RenderPreset.Custom;
                else
                    return RenderPreset.Opaque;
            }
            set
            {
                if (value == RenderPreset.Opaque) SetOpaque();
                else if (value == RenderPreset.Translucent) SetTranslucent();
                else if (value == RenderPreset.Transparent) SetTransparent();
                else if (value == RenderPreset.Custom) SetCustom();
                else
                    throw new System.Exception($"Unsupported preset mode {value}!");
            }
        }

        public GfxMaterial()
        {
            TextureCoords  = new GfxTextureCoord[3];
            TextureMappers = new GfxTextureMapper[3];

            Shader         = new GfxShaderReference();
            FragmentShader = new GfxFragShader();

            ShaderParameters = new List<GfxShaderParam>();

            Header.MagicNumber = 0x424F544D;
        }

        public void ConvertH3D(H3DMaterial material)
        {
            this.Name = material.Name;
            this.ShaderParameters = material.BcresShaderParams;
            this.MetaData = new GfxDict<GfxMetaData>();
            foreach (var usd in material.BcresUserData)
                this.MetaData.Add(usd);
            this.Flags = (GfxMaterialFlags)material.MaterialParams.Flags;
            this.FragmentShader.Lighting.TranslucencyKind = (GfxTranslucencyKind)material.MaterialParams.TranslucencyKind;
            this.TexCoordConfig = (GfxTexCoordConfig)material.MaterialParams.TexCoordConfig;
            this.LightSetIndex = material.MaterialParams.LightSetIndex;
            this.FogIndex = material.MaterialParams.FogIndex;
            this.Colors.Emission = material.MaterialParams.EmissionColor;
            this.Colors.Ambient = material.MaterialParams.AmbientColor;
            this.Colors.Diffuse = material.MaterialParams.DiffuseColor;
            this.Colors.Specular0 = material.MaterialParams.Specular0Color;
            this.Colors.Specular1 = material.MaterialParams.Specular1Color;
            this.Colors.Constant0 = material.MaterialParams.Constant0Color;
            this.Colors.Constant1 = material.MaterialParams.Constant1Color;
            this.Colors.Constant2 = material.MaterialParams.Constant2Color;
            this.Colors.Constant3 = material.MaterialParams.Constant3Color;
            this.Colors.Constant4 = material.MaterialParams.Constant4Color;
            this.Colors.Constant5 = material.MaterialParams.Constant5Color;
            this.Colors.Scale = material.MaterialParams.ColorScale;
            this.Rasterization.IsPolygonOffsetEnabled = false;
            this.RenderLayer = material.MaterialParams.RenderLayer;

            if (material.MaterialParams.Flags.HasFlag(H3DMaterialFlags.IsPolygonOffsetEnabled))
            {
                this.Rasterization.IsPolygonOffsetEnabled = true;
            }

            this.Rasterization.FaceCulling = material.MaterialParams.FaceCulling.ToGfxFaceCulling();
            this.Rasterization.PolygonOffsetUnit = material.MaterialParams.PolygonOffsetUnit;

            this.FragmentOperation.Depth.ColorMask = material.MaterialParams.DepthColorMask;
            this.FragmentOperation.Depth.Flags = material.BcresDepthFlags;

            //BCRES 
            this.FragmentOperation.Depth.ColorMask.RedWrite = false;
            this.FragmentOperation.Depth.ColorMask.GreenWrite = false;
            this.FragmentOperation.Depth.ColorMask.BlueWrite = false;
            this.FragmentOperation.Depth.ColorMask.AlphaWrite = false;
            this.FragmentOperation.Depth.ColorMask.DepthWrite = false;

            this.FragmentOperation.Blend.Mode = material.MaterialParams.BlendMode;
            this.FragmentOperation.Blend.ColorOperation = material.MaterialParams.ColorOperation;
            this.FragmentOperation.Blend.LogicalOperation = material.MaterialParams.LogicalOperation;
            this.FragmentOperation.Blend.Function = material.MaterialParams.BlendFunction;
            this.FragmentOperation.Blend.Color = material.MaterialParams.BlendColor;

            this.FragmentOperation.Stencil.Operation = material.MaterialParams.StencilOperation;
            this.FragmentOperation.Stencil.Test = material.MaterialParams.StencilTest;

            int TMIndex = 0;
            foreach (var texMap in material.TextureMappers)
            {
                int sourceIndex = (int)material.MaterialParams.TextureSources[TMIndex];
                string textureName = material.Texture0Name;
                if (TMIndex == 1) textureName = material.Texture1Name;
                if (TMIndex == 2) textureName = material.Texture2Name;

                if (string.IsNullOrEmpty(textureName))
                {
                    this.TextureMappers[TMIndex] = null;
                    TMIndex++;
                    continue;
                }

                var magFilter = PICATextureFilter.Linear;
                if (texMap.MagFilter == H3DTextureMagFilter.Linear)
                    magFilter = PICATextureFilter.Linear;
                if (texMap.MagFilter == H3DTextureMagFilter.Nearest)
                    magFilter = PICATextureFilter.Nearest;

                var minFilter = PICATextureFilter.Linear;
                var mipFilter = PICATextureFilter.Linear;

                if (texMap.MinFilter == H3DTextureMinFilter.Nearest)
                {
                    minFilter = PICATextureFilter.Nearest;
                }
                if (texMap.MinFilter == H3DTextureMinFilter.NearestMipmapNearest)
                {
                    minFilter = PICATextureFilter.Nearest;
                    mipFilter = PICATextureFilter.Nearest;
                }
                if (texMap.MinFilter == H3DTextureMinFilter.LinearMipmapLinear)
                {
                    minFilter = PICATextureFilter.Linear;
                    mipFilter = PICATextureFilter.Linear;
                }
                if (texMap.MinFilter == H3DTextureMinFilter.NearestMipmapLinear)
                {
                    minFilter = PICATextureFilter.Nearest;
                    mipFilter = PICATextureFilter.Linear;
                }
                if (texMap.MinFilter == H3DTextureMinFilter.LinearMipmapNearest)
                {
                    minFilter = PICATextureFilter.Linear;
                    mipFilter = PICATextureFilter.Nearest;
                }

                this.TextureMappers[TMIndex] = new GfxTextureMapper()
                {
                    BorderColor = texMap.BorderColor,
                    LODBias = texMap.LODBias,
                    MapperIndex = TMIndex,
                    MagFilter = magFilter,
                    MinFilter = minFilter,
                    MipFilter = mipFilter,
                    MinLOD = texMap.MinLOD,
                    WrapU = texMap.WrapU,
                    WrapV = texMap.WrapV,
                };
                this.TextureMappers[TMIndex].Texture = new GfxTextureReference()
                {
                    Path = textureName,
                    Name = "",
                };
                this.TextureMappers[TMIndex].Sampler = new GfxTextureSamplerStd()
                {
                    Parent = this.TextureMappers[TMIndex],
                    BorderColor = texMap.BorderColor.ToVector4(),
                    LODBias = texMap.LODBias,
                    MinFilter = GfxTextureMinFilter.Linear,
                };
                TMIndex++;
            }

            int TPIndex = 0;
            foreach (var texCoord in material.MaterialParams.TextureCoords)
            {
                int sourceIndex = (int)material.MaterialParams.TextureSources[TPIndex];
                if (texCoord.MappingType != H3DTextureMappingType.UvCoordinateMap)
                    sourceIndex = 0;

                this.TextureCoords[TPIndex] = new GfxTextureCoord()
                {
                    Scale = texCoord.Scale,
                    Rotation = texCoord.Rotation,
                    Translation = texCoord.Translation,
                    ReferenceCameraIndex = texCoord.ReferenceCameraIndex,
                    SourceCoordIndex = sourceIndex,
                    MappingType = (GfxTextureMappingType)texCoord.MappingType,
                    TransformType = (GfxTextureTransformType)texCoord.TransformType,
                    Transform = GfxTextureCoord.CalculateMatrix(texCoord.Scale, texCoord.Translation, texCoord.Rotation, (GfxTextureTransformType)texCoord.TransformType),
                };
                TPIndex++;
            }

            GfxFragmentFlags DstFlags = this.FragmentShader.Lighting.Flags;
            H3DFragmentFlags SrcFlags = material.MaterialParams.FragmentFlags;
            this.FragmentShader.Lighting.IsBumpRenormalize = false;

            DstFlags &= ~GfxFragmentFlags.IsClampHighLightEnabled;
            DstFlags &= ~GfxFragmentFlags.IsLUTDist0Enabled;
            DstFlags &= ~GfxFragmentFlags.IsLUTDist1Enabled;
            DstFlags &= ~GfxFragmentFlags.IsLUTGeoFactor0Enabled;
            DstFlags &= ~GfxFragmentFlags.IsLUTGeoFactor1Enabled;
            DstFlags &= ~GfxFragmentFlags.IsLUTReflectionEnabled;

            if (SrcFlags.HasFlag(H3DFragmentFlags.IsClampHighLightEnabled))
                DstFlags |= GfxFragmentFlags.IsClampHighLightEnabled;

            if (SrcFlags.HasFlag(H3DFragmentFlags.IsLUTDist0Enabled))
                DstFlags |= GfxFragmentFlags.IsLUTDist0Enabled;

            if (SrcFlags.HasFlag(H3DFragmentFlags.IsLUTDist1Enabled))
                DstFlags |= GfxFragmentFlags.IsLUTDist1Enabled;

            if (SrcFlags.HasFlag(H3DFragmentFlags.IsLUTGeoFactor0Enabled))
                DstFlags |= GfxFragmentFlags.IsLUTGeoFactor0Enabled;

            if (SrcFlags.HasFlag(H3DFragmentFlags.IsLUTGeoFactor1Enabled))
                DstFlags |= GfxFragmentFlags.IsLUTGeoFactor1Enabled;

            if (SrcFlags.HasFlag(H3DFragmentFlags.IsLUTReflectionEnabled))
                DstFlags |= GfxFragmentFlags.IsLUTReflectionEnabled;

            if (SrcFlags.HasFlag(H3DFragmentFlags.IsBumpRenormalizeEnabled))
                this.FragmentShader.Lighting.IsBumpRenormalize = true;

            this.FragmentShader.Lighting.Flags = DstFlags;

            this.FragmentShader.Lighting.FresnelSelector = (GfxFresnelSelector)material.MaterialParams.FresnelSelector;
            this.FragmentShader.Lighting.BumpTexture = material.MaterialParams.BumpTexture;
            this.FragmentShader.Lighting.BumpMode = (GfxBumpMode)material.MaterialParams.BumpMode;

            GfxFragLightLUT AssignLut(PICALUTInput input, PICALUTScale scale, string table, string sampler)
            {
                if (string.IsNullOrEmpty(sampler))
                {
                    return null;
                }

               return new GfxFragLightLUT()
                {
                   Input = input,
                   Scale = scale,
                   Sampler = new GfxLUTReference()
                   {
                       SamplerName = sampler,
                       TableName = table,
                   },
                };
            }

            var mparams = material.MaterialParams;

            this.FragmentShader.LUTs.ReflecR = AssignLut(
                mparams.LUTInputSelection.ReflecR,
                mparams.LUTInputScale.ReflecR,
                mparams.LUTReflecRTableName,
                mparams.LUTReflecRSamplerName);

            this.FragmentShader.LUTs.ReflecG = AssignLut(
                mparams.LUTInputSelection.ReflecG,
                mparams.LUTInputScale.ReflecG,
                mparams.LUTReflecGTableName,
                mparams.LUTReflecGSamplerName);

            this.FragmentShader.LUTs.ReflecB = AssignLut(
                mparams.LUTInputSelection.ReflecB,
                mparams.LUTInputScale.ReflecB,
                mparams.LUTReflecBTableName,
                mparams.LUTReflecBSamplerName);

            this.FragmentShader.LUTs.Fresnel = AssignLut( 
                mparams.LUTInputSelection.Fresnel,
                mparams.LUTInputScale.Fresnel,
                mparams.LUTFresnelTableName,
                mparams.LUTFresnelSamplerName);

            this.FragmentShader.LUTs.Dist0 = AssignLut(
                mparams.LUTInputSelection.Dist0,
                mparams.LUTInputScale.Dist0,
                mparams.LUTDist0TableName,
                mparams.LUTDist0SamplerName);

            this.FragmentShader.LUTs.Dist1 = AssignLut(
                mparams.LUTInputSelection.Dist1,
                mparams.LUTInputScale.Dist1,
                mparams.LUTDist1TableName,
                mparams.LUTDist1SamplerName);

            for (int i = 0; i < 6; i++)
            {
                this.FragmentShader.TextureEnvironments[i] = new GfxTexEnv();
                this.FragmentShader.TextureEnvironments[i].Stage = material.MaterialParams.TexEnvStages[i];
                this.FragmentShader.TextureEnvironments[i].Constant = material.MaterialParams.TexEnvStages[i].Constant;
            }


            this.FragmentShader.AlphaTest.Test = material.MaterialParams.AlphaTest;
            this.FragmentShader.TexEnvBufferColor = material.MaterialParams.TexEnvBufferColor;

            this.UsedTextureCoordsCount = this.TextureCoords.Where(x => x.Scale.X != 0).ToList().Count;

            this.Shader.Name = "";
            this.Shader.Path = "DefaultShader";
        }

        public CtrH3D.Model.Material.H3DMaterial ToH3D(string modelName)
        {
            H3DMaterial Mat = new H3DMaterial() { Name = this.Name };
            Mat.BcresShaderParams = this.ShaderParameters;
            Mat.BcresUserData = new GfxDict<GfxMetaData>();
            foreach (var usd in this.MetaData)
                Mat.BcresUserData.Add(usd);

            Mat.MaterialParams.ModelReference = $"{Mat.Name}@{modelName}";
            Mat.MaterialParams.ShaderReference = "0@DefaultShader";
            Mat.MaterialParams.LightSetIndex = (ushort)this.LightSetIndex;
            Mat.MaterialParams.FogIndex = (ushort)this.FogIndex;

            Mat.MaterialParams.Flags = (H3DMaterialFlags)this.Flags;

            Mat.MaterialParams.RenderLayer = this.RenderLayer;
            Mat.MaterialParams.TexCoordConfig = (H3DTexCoordConfig)this.TexCoordConfig;

            Mat.MaterialParams.TranslucencyKind = (H3DTranslucencyKind)this.FragmentShader.Lighting.TranslucencyKind;
            Mat.MaterialParams.FresnelSelector = (H3DFresnelSelector)this.FragmentShader.Lighting.FresnelSelector;

            Mat.MaterialParams.EmissionColor = this.Colors.Emission;
            Mat.MaterialParams.AmbientColor = this.Colors.Ambient;
            Mat.MaterialParams.DiffuseColor = this.Colors.Diffuse;
            Mat.MaterialParams.Specular0Color = this.Colors.Specular0;
            Mat.MaterialParams.Specular1Color = this.Colors.Specular1;
            Mat.MaterialParams.Constant0Color = this.Colors.Constant0;
            Mat.MaterialParams.Constant1Color = this.Colors.Constant1;
            Mat.MaterialParams.Constant2Color = this.Colors.Constant2;
            Mat.MaterialParams.Constant3Color = this.Colors.Constant3;
            Mat.MaterialParams.Constant4Color = this.Colors.Constant4;
            Mat.MaterialParams.Constant5Color = this.Colors.Constant5;
            Mat.MaterialParams.ColorScale = this.Colors.Scale;

            if (this.Rasterization.IsPolygonOffsetEnabled)
            {
                Mat.MaterialParams.Flags |= H3DMaterialFlags.IsPolygonOffsetEnabled;
            }

            Mat.MaterialParams.FaceCulling = this.Rasterization.FaceCulling.ToPICAFaceCulling();
            Mat.MaterialParams.PolygonOffsetUnit = this.Rasterization.PolygonOffsetUnit;

            Mat.MaterialParams.DepthColorMask = this.FragmentOperation.Depth.ColorMask;
            Mat.BcresDepthFlags = this.FragmentOperation.Depth.Flags;

            Mat.MaterialParams.DepthColorMask.RedWrite = true;
            Mat.MaterialParams.DepthColorMask.GreenWrite = true;
            Mat.MaterialParams.DepthColorMask.BlueWrite = true;
            Mat.MaterialParams.DepthColorMask.AlphaWrite = true;
            Mat.MaterialParams.DepthColorMask.DepthWrite = true;

            Mat.MaterialParams.ColorBufferRead = false;
            Mat.MaterialParams.ColorBufferWrite = true;

            Mat.MaterialParams.StencilBufferRead = false;
            Mat.MaterialParams.StencilBufferWrite = false;

            Mat.MaterialParams.DepthBufferRead = true;
            Mat.MaterialParams.DepthBufferWrite = true;

            Mat.MaterialParams.ColorOperation = this.FragmentOperation.Blend.ColorOperation;
            Mat.MaterialParams.LogicalOperation = this.FragmentOperation.Blend.LogicalOperation;
            Mat.MaterialParams.BlendFunction = this.FragmentOperation.Blend.Function;
            Mat.MaterialParams.BlendColor = this.FragmentOperation.Blend.Color;
            Mat.MaterialParams.BlendMode = this.FragmentOperation.Blend.Mode;

            Mat.MaterialParams.StencilOperation = this.FragmentOperation.Stencil.Operation;
            Mat.MaterialParams.StencilTest = this.FragmentOperation.Stencil.Test;

            int TCIndex = 0;

            foreach (GfxTextureCoord TexCoord in this.TextureCoords)
            {
                H3DTextureCoord TC = new H3DTextureCoord();

                TC.MappingType = (H3DTextureMappingType)TexCoord.MappingType;

                TC.ReferenceCameraIndex = (sbyte)TexCoord.ReferenceCameraIndex;

                TC.TransformType = (H3DTextureTransformType)TexCoord.TransformType;

                TC.Scale = TexCoord.Scale;
                TC.Rotation = TexCoord.Rotation;
                TC.Translation = TexCoord.Translation;

                switch (TexCoord.MappingType)
                {
                    case GfxTextureMappingType.UvCoordinateMap:
                        Mat.MaterialParams.TextureSources[TCIndex] = TexCoord.SourceCoordIndex;
                        break;

                    case GfxTextureMappingType.CameraCubeEnvMap:
                        Mat.MaterialParams.TextureSources[TCIndex] = 3;
                        break;

                    case GfxTextureMappingType.CameraSphereEnvMap:
                        Mat.MaterialParams.TextureSources[TCIndex] = 4;
                        break;
                }

                Mat.MaterialParams.TextureCoords[TCIndex++] = TC;

                if (TCIndex == this.UsedTextureCoordsCount) break;
            }

            int TMIndex = 0;

            foreach (GfxTextureMapper TexMapper in this.TextureMappers)
            {
                if (TexMapper == null) break;

                H3DTextureMapper TM = new H3DTextureMapper();

                TM.WrapU = TexMapper.WrapU;
                TM.WrapV = TexMapper.WrapV;

                TM.MagFilter = (H3DTextureMagFilter)TexMapper.MinFilter;

                switch ((uint)TexMapper.MagFilter | ((uint)TexMapper.MipFilter << 1))
                {
                    case 0: TM.MinFilter = H3DTextureMinFilter.NearestMipmapNearest; break;
                    case 1: TM.MinFilter = H3DTextureMinFilter.LinearMipmapNearest; break;
                    case 2: TM.MinFilter = H3DTextureMinFilter.NearestMipmapLinear; break;
                    case 3: TM.MinFilter = H3DTextureMinFilter.LinearMipmapLinear; break;
                }

                TM.LODBias = TexMapper.LODBias;
                TM.MinLOD = TexMapper.MinLOD;

                TM.BorderColor = TexMapper.BorderColor;

                Mat.TextureMappers[TMIndex++] = TM;
            }

            Mat.EnabledTextures[0] = this.TextureMappers[0] != null;
            Mat.EnabledTextures[1] = this.TextureMappers[1] != null;
            Mat.EnabledTextures[2] = this.TextureMappers[2] != null;

            Mat.Texture0Name = this.TextureMappers[0]?.Texture.Path;
            Mat.Texture1Name = this.TextureMappers[1]?.Texture.Path;
            Mat.Texture2Name = this.TextureMappers[2]?.Texture.Path;

            GfxFragmentFlags SrcFlags = this.FragmentShader.Lighting.Flags;
            H3DFragmentFlags DstFlags = 0;

            if ((SrcFlags & GfxFragmentFlags.IsClampHighLightEnabled) != 0)
                DstFlags |= H3DFragmentFlags.IsClampHighLightEnabled;

            if ((SrcFlags & GfxFragmentFlags.IsLUTDist0Enabled) != 0)
                DstFlags |= H3DFragmentFlags.IsLUTDist0Enabled;

            if ((SrcFlags & GfxFragmentFlags.IsLUTDist1Enabled) != 0)
                DstFlags |= H3DFragmentFlags.IsLUTDist1Enabled;

            if ((SrcFlags & GfxFragmentFlags.IsLUTGeoFactor0Enabled) != 0)
                DstFlags |= H3DFragmentFlags.IsLUTGeoFactor0Enabled;

            if ((SrcFlags & GfxFragmentFlags.IsLUTGeoFactor1Enabled) != 0)
                DstFlags |= H3DFragmentFlags.IsLUTGeoFactor1Enabled;

            if ((SrcFlags & GfxFragmentFlags.IsLUTReflectionEnabled) != 0)
                DstFlags |= H3DFragmentFlags.IsLUTReflectionEnabled;

            if (this.FragmentShader.Lighting.IsBumpRenormalize)
                DstFlags |= H3DFragmentFlags.IsBumpRenormalizeEnabled;

            Mat.MaterialParams.FragmentFlags = DstFlags;

            Mat.MaterialParams.FresnelSelector = (H3DFresnelSelector)this.FragmentShader.Lighting.FresnelSelector;

            Mat.MaterialParams.BumpTexture = (byte)this.FragmentShader.Lighting.BumpTexture;

            Mat.MaterialParams.BumpMode = (H3DBumpMode)this.FragmentShader.Lighting.BumpMode;

            Mat.MaterialParams.LUTInputSelection.ReflecR = this.FragmentShader.LUTs.ReflecR?.Input ?? 0;
            Mat.MaterialParams.LUTInputSelection.ReflecG = this.FragmentShader.LUTs.ReflecG?.Input ?? 0;
            Mat.MaterialParams.LUTInputSelection.ReflecB = this.FragmentShader.LUTs.ReflecB?.Input ?? 0;
            Mat.MaterialParams.LUTInputSelection.Dist0 = this.FragmentShader.LUTs.Dist0?.Input ?? 0;
            Mat.MaterialParams.LUTInputSelection.Dist1 = this.FragmentShader.LUTs.Dist1?.Input ?? 0;
            Mat.MaterialParams.LUTInputSelection.Fresnel = this.FragmentShader.LUTs.Fresnel?.Input ?? 0;

            Mat.MaterialParams.LUTInputScale.ReflecR = this.FragmentShader.LUTs.ReflecR?.Scale ?? 0;
            Mat.MaterialParams.LUTInputScale.ReflecG = this.FragmentShader.LUTs.ReflecG?.Scale ?? 0;
            Mat.MaterialParams.LUTInputScale.ReflecB = this.FragmentShader.LUTs.ReflecB?.Scale ?? 0;
            Mat.MaterialParams.LUTInputScale.Dist0 = this.FragmentShader.LUTs.Dist0?.Scale ?? 0;
            Mat.MaterialParams.LUTInputScale.Dist1 = this.FragmentShader.LUTs.Dist1?.Scale ?? 0;
            Mat.MaterialParams.LUTInputScale.Fresnel = this.FragmentShader.LUTs.Fresnel?.Scale ?? 0;

            Mat.MaterialParams.LUTReflecRTableName = this.FragmentShader.LUTs.ReflecR?.Sampler.TableName;
            Mat.MaterialParams.LUTReflecGTableName = this.FragmentShader.LUTs.ReflecG?.Sampler.TableName;
            Mat.MaterialParams.LUTReflecBTableName = this.FragmentShader.LUTs.ReflecB?.Sampler.TableName;
            Mat.MaterialParams.LUTDist0TableName = this.FragmentShader.LUTs.Dist0?.Sampler.TableName;
            Mat.MaterialParams.LUTDist1TableName = this.FragmentShader.LUTs.Dist1?.Sampler.TableName;
            Mat.MaterialParams.LUTFresnelTableName = this.FragmentShader.LUTs.Fresnel?.Sampler.TableName;

            Mat.MaterialParams.LUTReflecRSamplerName = this.FragmentShader.LUTs.ReflecR?.Sampler.SamplerName;
            Mat.MaterialParams.LUTReflecGSamplerName = this.FragmentShader.LUTs.ReflecG?.Sampler.SamplerName;
            Mat.MaterialParams.LUTReflecBSamplerName = this.FragmentShader.LUTs.ReflecB?.Sampler.SamplerName;
            Mat.MaterialParams.LUTDist0SamplerName = this.FragmentShader.LUTs.Dist0?.Sampler.SamplerName;
            Mat.MaterialParams.LUTDist1SamplerName = this.FragmentShader.LUTs.Dist1?.Sampler.SamplerName;
            Mat.MaterialParams.LUTFresnelSamplerName = this.FragmentShader.LUTs.Fresnel?.Sampler.SamplerName;

            for (int i = 0; i < 6; i++)
            {
                Mat.MaterialParams.TexEnvStages[i] = this.FragmentShader.TextureEnvironments[i].Stage;
                Mat.MaterialParams.TexEnvStages[i].Constant = this.FragmentShader.TextureEnvironments[i].Constant;
            }

            Mat.MaterialParams.AlphaTest = this.FragmentShader.AlphaTest.Test;

            Mat.MaterialParams.TexEnvBufferColor = this.FragmentShader.TexEnvBufferColor;

            void SetConstant(GfxTexEnv texEnv, PICATexEnvStage stage)
            {
                switch (texEnv.Constant)
                {
                    case GfxTexEnvConstant.Constant0: stage.Color = this.Colors.Constant0; break;
                    case GfxTexEnvConstant.Constant1: stage.Color = this.Colors.Constant1; break;
                    case GfxTexEnvConstant.Constant2: stage.Color = this.Colors.Constant2; break;
                    case GfxTexEnvConstant.Constant3: stage.Color = this.Colors.Constant3; break;
                    case GfxTexEnvConstant.Constant4: stage.Color = this.Colors.Constant4; break;
                    case GfxTexEnvConstant.Constant5: stage.Color = this.Colors.Constant5; break;
                }
            }

            SetConstant(this.FragmentShader.TextureEnvironments[0], Mat.MaterialParams.TexEnvStages[0]);
            SetConstant(this.FragmentShader.TextureEnvironments[1], Mat.MaterialParams.TexEnvStages[1]);
            SetConstant(this.FragmentShader.TextureEnvironments[2], Mat.MaterialParams.TexEnvStages[2]);
            SetConstant(this.FragmentShader.TextureEnvironments[3], Mat.MaterialParams.TexEnvStages[3]);
            SetConstant(this.FragmentShader.TextureEnvironments[4], Mat.MaterialParams.TexEnvStages[4]);
            SetConstant(this.FragmentShader.TextureEnvironments[5], Mat.MaterialParams.TexEnvStages[5]);

            return Mat;
        }

        public static GfxMaterial CreateDefault()
        {
            var material = new GfxMaterial();
            material.Name = "Default";
            material.Flags = GfxMaterialFlags.IsFragmentLightingEnabled;
            material.Shader = new GfxShaderReference()
            {
                Name = "",
                Path = "DefaultShader",
            };
            material.TexCoordConfig = GfxTexCoordConfig.Config0120;
            material.RenderLayer = 0;
            material.Colors.Emission = new RGBA(0, 0, 0, 0);
            material.Colors.Ambient = new RGBA(255, 255, 255, 0);
            material.Colors.Diffuse = new RGBA(255, 255, 255, 255);
            material.Colors.Specular0 = new RGBA(64, 64, 64, 0);
            material.Colors.Specular1 = new RGBA(0, 0, 0, 0);
            material.Colors.Constant0 = new RGBA(0, 0, 0, 255);
            material.Colors.Constant1 = new RGBA(0, 0, 0, 255);
            material.Colors.Constant2 = new RGBA(0, 0, 0, 255);
            material.Colors.Constant3 = new RGBA(0, 0, 0, 255);
            material.Colors.Constant4 = new RGBA(0, 0, 0, 255);
            material.Colors.Constant5 = new RGBA(0, 0, 0, 255);
            material.Colors.Scale = 1.0f;
            material.Rasterization = new GfxRasterization()
            {
                FaceCulling = GfxFaceCulling.BackFace,
                IsPolygonOffsetEnabled = false,
                PolygonOffsetUnit = 0.0f,
            };
            material.FragmentOperation = new GfxFragOp()
            {
                Depth = new GfxFragOpDepth()
                {
                    Flags = (GfxFragOpDepthFlags)3,
                    ColorMask = new PICADepthColorMask()
                    {
                        Enabled = true,
                        DepthFunc = PICATestFunc.Less,
                        DepthWrite = false,
                        RedWrite = false, BlueWrite = false, GreenWrite = false, AlphaWrite = false,
                    },
                },
                Blend = new GfxFragOpBlend()
                {
                    Mode = GfxFragOpBlendMode.None,
                    ColorOperation = new PICAColorOperation()
                    {
                        FragOpMode = PICAFragOpMode.Default,
                        BlendMode = PICABlendMode.Blend,
                    },
                    Function = new PICABlendFunction()
                    {
                        ColorEquation = PICABlendEquation.FuncAdd,
                        AlphaEquation = PICABlendEquation.FuncAdd,
                        ColorSrcFunc = PICABlendFunc.One,
                        ColorDstFunc = PICABlendFunc.Zero,
                        AlphaSrcFunc = PICABlendFunc.One,
                        AlphaDstFunc = PICABlendFunc.Zero,
                    },
                    LogicalOperation = PICALogicalOp.Clear,
                    Color = new RGBA(0, 0, 0, 255),
                },
                Stencil = new GfxFragOpStencil()
                {
                    Test = new PICAStencilTest() { Mask = 255, },
                    Operation = new PICAStencilOperation(),
                },
            };

            for (int i = 0; i < 3; i++)
                material.TextureCoords[i] = GfxTextureCoord.Default;
            for (int i = 0; i < 3; i++)
                material.TextureMappers[i] = null;

            material.UsedTextureCoordsCount = 1;
            material.TextureMappers[0] = new GfxTextureMapper("Default");
            material.LightSetIndex = 0;
            material.FogIndex = 0;
            material.ShaderProgramDescIndex = 0;
            material.FragmentShader = new GfxFragShader();
            material.FragmentShader.TexEnvBufferColor = new RGBA(0, 0, 0, 255);
            material.FragmentShader.LUTs = new GfxFragLightLUTs() {};
            for (int i = 0; i < 6; i++)
            {
                material.FragmentShader.TextureEnvironments[i] = new GfxTexEnv();
                material.FragmentShader.TextureEnvironments[i].Stage = PICATexEnvStage.PassThrough2;
            }

            material.FragmentShader.TextureEnvironments[0].Stage = PICATexEnvStage.Texture0;
            material.FragmentShader.TextureEnvironments[0].Constant = GfxTexEnvConstant.Constant0;

            material.FragmentShader.Lighting = new GfxFragLight()
            {
                Flags = (GfxFragmentFlags)0,
                BumpTexture = 0,
                TranslucencyKind = GfxTranslucencyKind.Layer0,
                FresnelSelector = GfxFresnelSelector.No,
                IsBumpRenormalize = false,
                BumpMode = 0,
            };
            material.FragmentShader.AlphaTest = new GfxAlphaTest()
            {
                Test = new PICAAlphaTest()
                {
                    Enabled = false,
                    Function = PICATestFunc.Always,
                    Reference = 128,
                },
            };


            var firstStage = new PICATexEnvStage()
            {
                Source = new PICATexEnvSource()
                {
                    Color = new PICATextureCombinerSource[3]
                       {
                           PICATextureCombinerSource.FragmentPrimaryColor,
                           PICATextureCombinerSource.Texture0,
                           PICATextureCombinerSource.Constant,
                       },
                    Alpha = new PICATextureCombinerSource[3]
                       {
                           PICATextureCombinerSource.FragmentPrimaryColor,
                           PICATextureCombinerSource.Texture0,
                           PICATextureCombinerSource.Constant,
                       },
                },
                Operand = new PICATexEnvOperand()
                {
                    Color = new PICATextureCombinerColorOp[3]
                       {
                           PICATextureCombinerColorOp.Color,
                           PICATextureCombinerColorOp.Color,
                           PICATextureCombinerColorOp.Color,
                       },
                    Alpha = new PICATextureCombinerAlphaOp[3]
                       {
                           PICATextureCombinerAlphaOp.Alpha,
                           PICATextureCombinerAlphaOp.Alpha,
                           PICATextureCombinerAlphaOp.Alpha,
                       },
                },
                Combiner = new PICATexEnvCombiner()
                {
                    Color = PICATextureCombinerMode.Modulate,
                    Alpha = PICATextureCombinerMode.Modulate,
                },
                Scale = new PICATexEnvScale()
                {
                    Color = PICATextureCombinerScale.One,
                    Alpha = PICATextureCombinerScale.One,
                },
                Color = new RGBA(0, 0, 0, 255),
                UpdateAlphaBuffer = false,
                UpdateColorBuffer = false,
            };
            material.FragmentShader.TextureEnvironments[0].Stage = firstStage;

            return material;
        }

        public void SetOpaque()
        {
            this.RenderLayer = 0;
            this.FragmentOperation.Depth.ColorMask.Enabled = true;
            this.FragmentOperation.Depth.ColorMask.DepthWrite = true;
            this.FragmentOperation.Depth.ColorMask.DepthFunc = PICATestFunc.Less;

            this.FragmentShader.AlphaTest.Test.Enabled = false;

            this.FragmentOperation.Blend = new GfxFragOpBlend()
            {
                Mode = GfxFragOpBlendMode.None,
                ColorOperation = new PICAColorOperation()
                {
                    FragOpMode = PICAFragOpMode.Default,
                    BlendMode = PICABlendMode.Blend,
                },
                Function = new PICABlendFunction()
                {
                    ColorEquation = PICABlendEquation.FuncAdd,
                    AlphaEquation = PICABlendEquation.FuncAdd,
                    ColorSrcFunc = PICABlendFunc.One,
                    ColorDstFunc = PICABlendFunc.Zero,
                    AlphaSrcFunc = PICABlendFunc.One,
                    AlphaDstFunc = PICABlendFunc.Zero,
                },
                LogicalOperation = PICALogicalOp.Clear,
                Color = new RGBA(0, 0, 0, 255),
            };
        }

        public void SetCustom()
        {
            this.RenderLayer = 2;
            this.FragmentOperation.Depth.ColorMask.Enabled = true;
            this.FragmentOperation.Depth.ColorMask.DepthWrite = false;
            this.FragmentOperation.Depth.ColorMask.DepthFunc = PICATestFunc.Less;

            this.FragmentShader.AlphaTest.Test.Enabled = false;

            this.FragmentOperation.Blend.Mode = GfxFragOpBlendMode.BlendSeparate;
            this.FragmentOperation.Blend.ColorOperation.FragOpMode = PICAFragOpMode.Default;
            this.FragmentOperation.Blend.ColorOperation.BlendMode = PICABlendMode.Blend;
            this.FragmentOperation.Blend.Function.ColorSrcFunc = PICABlendFunc.SourceAlpha;
            this.FragmentOperation.Blend.Function.ColorDstFunc = PICABlendFunc.OneMinusSourceAlpha;
            this.FragmentOperation.Blend.Function.AlphaSrcFunc = PICABlendFunc.SourceAlpha;
            this.FragmentOperation.Blend.Function.AlphaDstFunc = PICABlendFunc.OneMinusSourceAlpha;
            this.FragmentOperation.Blend.Function.AlphaEquation = PICABlendEquation.FuncAdd;
            this.FragmentOperation.Blend.Function.ColorEquation = PICABlendEquation.FuncAdd;
        }

        public void SetTransparent()
        {
            this.RenderLayer = 0;
            this.FragmentOperation.Depth.ColorMask.Enabled = true;
            this.FragmentOperation.Depth.ColorMask.DepthWrite = true;
            this.FragmentOperation.Depth.ColorMask.DepthFunc = PICATestFunc.Less;

            this.FragmentShader.AlphaTest.Test.Enabled = true;
            this.FragmentShader.AlphaTest.Test.Reference = 128;
            this.FragmentShader.AlphaTest.Test.Function = PICATestFunc.Greater;

            this.FragmentOperation.Blend = new GfxFragOpBlend()
            {
                Mode = GfxFragOpBlendMode.None,
                ColorOperation = new PICAColorOperation()
                {
                    FragOpMode = PICAFragOpMode.Default,
                    BlendMode = PICABlendMode.Blend,
                },
                Function = new PICABlendFunction()
                {
                    ColorEquation = PICABlendEquation.FuncAdd,
                    AlphaEquation = PICABlendEquation.FuncAdd,
                    ColorSrcFunc = PICABlendFunc.One,
                    ColorDstFunc = PICABlendFunc.Zero,
                    AlphaSrcFunc = PICABlendFunc.One,
                    AlphaDstFunc = PICABlendFunc.Zero,
                },
                LogicalOperation = PICALogicalOp.Clear,
                Color = new RGBA(0, 0, 0, 255),
            };
        }

        public void SetTranslucent()
        {
            this.RenderLayer = 1;
            this.FragmentOperation.Depth.ColorMask.Enabled = true;
            this.FragmentOperation.Depth.ColorMask.DepthWrite = false;
            this.FragmentOperation.Depth.ColorMask.DepthFunc = PICATestFunc.Less;

            this.FragmentShader.AlphaTest.Test.Enabled = false;

            this.FragmentOperation.Blend.Mode = GfxFragOpBlendMode.Blend;
            this.FragmentOperation.Blend.ColorOperation.FragOpMode = PICAFragOpMode.Default;
            this.FragmentOperation.Blend.ColorOperation.BlendMode = PICABlendMode.Blend;
            this.FragmentOperation.Blend.Function.ColorSrcFunc = PICABlendFunc.SourceAlpha;
            this.FragmentOperation.Blend.Function.ColorDstFunc = PICABlendFunc.OneMinusSourceAlpha;
            this.FragmentOperation.Blend.Function.AlphaSrcFunc = PICABlendFunc.SourceAlpha;
            this.FragmentOperation.Blend.Function.AlphaDstFunc = PICABlendFunc.OneMinusSourceAlpha;
            this.FragmentOperation.Blend.Function.AlphaEquation = PICABlendEquation.FuncAdd;
            this.FragmentOperation.Blend.Function.ColorEquation = PICABlendEquation.FuncAdd;
        }
        void ICustomSerialization.Deserialize(BinaryDeserializer Deserializer) { }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            if (TextureMappers.Length != 3) throw new System.Exception($"Invalid texture mappers length!");
            if (TextureCoords.Length != 3) throw new System.Exception($"Invalid texture coord length!");

            for (int i = 0; i < 3 && TextureMappers[i] != null; i++)
            {
                TextureMappers[i].MapperIndex = i;
            }

            for (int i = 0; i < 6 && FragmentShader.TextureEnvironments[i] != null; i++)
            {
                FragmentShader.TextureEnvironments[i].StageIndex = i;
            }

            CalcMaterialFlagsHash();
            CalcShaderParamsHash();
            CalcTextureCoordsHash();
            CalcTextureSamplersHash();
            CalcMaterialColorsHash();
            CalcRasterizationsHash();
            CalcFragLightHash();
            CalcFragLightLUTSampHash();
            CalcTextureEnvironmentHash();
            CalcAlphaTestHash();
            CalcFragOpHash();

            return false;
        }

        private void CalcMaterialFlagsHash()
        {
            using (MemoryStream MS = new MemoryStream())
            {
                BinaryWriter Writer = new BinaryWriter(MS);

                Writer.Write((uint)(Flags | GfxMaterialFlags.IsPolygonOffsetEnabled));

                MaterialFlagsHash = HashBuffer(MS.ToArray());
            }
        }

        private void CalcShaderParamsHash()
        {
            ShaderParamsHash = HashBuffer(new byte[0]); //TODO
        }

        private void CalcTextureCoordsHash()
        {
            using (MemoryStream MS = new MemoryStream())
            {
                BinaryWriter Writer = new BinaryWriter(MS);

                Writer.Write((uint)TexCoordConfig);

                for (int i = 0; i < 3; i++)
                {
                    Writer.Write(TextureCoords[i].GetBytes(i >= UsedTextureCoordsCount));
                    Writer.Write((uint)TexCoordConfig);
                }

                TextureCoordsHash = HashBuffer(MS.ToArray());
            }
        }

        private void CalcTextureSamplersHash()
        {
            uint[] Wraps = new uint[] { 2, 3, 0, 1 };

            using (MemoryStream MS = new MemoryStream())
            {
                BinaryWriter Writer = new BinaryWriter(MS);

                Writer.Write((uint)TexCoordConfig);

                foreach (GfxTextureMapper TexMapper in TextureMappers)
                {
                    if (TexMapper != null && TexMapper.Sampler is GfxTextureSamplerStd)
                    {
                        Writer.Write(TexMapper.BorderColor.ToVector4());
                        Writer.Write(Wraps[(uint)TexMapper.WrapU]);
                        Writer.Write(Wraps[(uint)TexMapper.WrapV]);
                        Writer.Write((float)TexMapper.MinLOD);
                        Writer.Write(TexMapper.LODBias);
                        Writer.Write((uint)TexMapper.GetMinFilter());
                        Writer.Write((uint)TexMapper.MagFilter);
                    }
                }

                TextureSamplersHash = HashBuffer(MS.ToArray());
            }
        }

        private void CalcMaterialColorsHash()
        {
            MaterialColorsHash = HashBuffer(Colors.GetBytes());
        }

        private void CalcRasterizationsHash()
        {
            RasterizationHash = HashBuffer(Rasterization.GetBytes());
        }

        private void CalcFragLightHash()
        {
            using (MemoryStream MS = new MemoryStream())
            {
                BinaryWriter Writer = new BinaryWriter(MS);

                byte FragLightEnb = (Flags & GfxMaterialFlags.IsFragmentLightingEnabled) != 0
                    ? (byte)1
                    : (byte)0;

                Writer.Write(FragmentShader.Lighting.GetBytes());
                Writer.Write(FragLightEnb);

                FragLightHash = HashBuffer(MS.ToArray());
            }   
        }

        private void CalcFragLightLUTSampHash()
        {
            FragLightLUTSampHash = HashBuffer(FragmentShader.LUTs.GetBytes());
        }

        private void CalcTextureEnvironmentHash()
        {
            using (MemoryStream MS = new MemoryStream())
            {
                BinaryWriter Writer = new BinaryWriter(MS);

                Writer.Write(FragmentShader.TexEnvBufferColor.ToVector4());

                foreach (GfxTexEnv TexEnv in FragmentShader.TextureEnvironments)
                {
                    Writer.Write(TexEnv.GetBytes());
                }

                TextureEnvironmentHash = HashBuffer(MS.ToArray());
            }
        }

        private void CalcAlphaTestHash()
        {
            AlphaTestHash = HashBuffer(FragmentShader.AlphaTest.GetBytes());
        }

        private void CalcFragOpHash()
        {
            FragOpHash = HashBuffer(FragmentOperation.GetBytes());
        }

        private uint HashBuffer(byte[] Input)
        {
            using (MD5CryptoServiceProvider MD5 = new MD5CryptoServiceProvider())
            {
                byte[] Buffer = MD5.ComputeHash(Input);

                uint Hash = 0;

                for (int i = 0; i < Buffer.Length; i++)
                {
                    Hash ^= (uint)Buffer[i] << (i & 3) * 8;
                }

                return Hash != 0 ? Hash : 1;
            }
        }

        public enum RenderPreset
        {
            Opaque,
            Transparent,
            Translucent,
            Custom,
        }
    }
}
