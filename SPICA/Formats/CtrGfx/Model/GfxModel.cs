using SPICA.Formats.CtrGfx.Model.Material;
using SPICA.Formats.CtrGfx.Model.Mesh;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;
using SPICA.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SPICA.Formats.CtrGfx.Model
{
    [TypeChoice(0x40000012u, typeof(GfxModel))]
    [TypeChoice(0x40000092u, typeof(GfxModelSkeletal))]
    public class GfxModel : GfxNodeTransform
    {
        [Ignore]
        [Newtonsoft.Json.JsonIgnore]
        public CtrH3D.Model.H3DModel H3DModel;

        public List<GfxMesh> Meshes;

        public GfxDict<GfxMaterial> Materials;

        public List<GfxShape> Shapes;

        public GfxDict<GfxMeshNodeVisibility> MeshNodeVisibilities;

        public GfxModelFlags Flags;

        public PICAFaceCulling FaceCulling;

        public int LayerId;

        public GfxModel()
        {
            Meshes = new List<GfxMesh>();

            Materials = new GfxDict<GfxMaterial>();

            Shapes = new List<GfxShape>();

            MeshNodeVisibilities = new GfxDict<GfxMeshNodeVisibility>();

            this.Header.MagicNumber = 0x4C444D43;
            this.Header.Revision = 150994944;

            Flags = GfxModelFlags.IsVisible;
            IsBranchVisible = true;

            this.TransformScale = new Vector3(1);
            this.LocalTransform = Math3D.Matrix3x4.Identity;
            this.WorldTransform = Math3D.Matrix3x4.Identity;
            this.TransformScale = Vector3.One;
            this.IsBranchVisible = true;
        }

        public H3DModel ToH3D()
        {
            var Model = this;

            CtrH3D.Model.H3DModel Mdl = new CtrH3D.Model.H3DModel();
            Model.H3DModel = Mdl;
            Mdl.Name = Model.Name;

            Mdl.WorldTransform = Model.WorldTransform;

            foreach (GfxMaterial Material in Model.Materials)
            {
                Material.H3DMaterial = Material.ToH3D(Model.Name);
                Mdl.Materials.Add(Material.H3DMaterial);
            }

            foreach (GfxMesh Mesh in Model.Meshes)
            {
                GfxShape Shape = Model.Shapes[Mesh.ShapeIndex];

                H3DMesh M = new H3DMesh();

                PICAVertex[] Vertices = null;

                foreach (GfxVertexBuffer VertexBuffer in Shape.VertexBuffers)
                {
                    /*
                     * CGfx supports 3 types of vertex buffer:
                     * - Non-Interleaved: Each attribute is stored on it's on stream, like this:
                     * P0 P1 P2 P3 P4 P5 ... N0 N1 N2 N3 N4 N5
                     * - Interleaved: All attributes are stored on the same stream, like this:
                     * P0 N0 P1 N1 P2 N2 P3 N3 P4 N4 P5 N5 ...
                     * - Fixed: The attribute have only a single fixed value, so instead of a stream,
                     * it have a single vector.
                     */
                    if (VertexBuffer is GfxAttribute)
                    {
                        //Non-Interleaved buffer
                        GfxAttribute Attr = (GfxAttribute)VertexBuffer;

                        M.Attributes.Add(Attr.ToPICAAttribute());

                        Vector4[] Vectors = Attr.GetVectors();

                        if (Vertices == null)
                        {
                            Vertices = new PICAVertex[Vectors.Length];
                        }

                        for (int i = 0; i < Vectors.Length; i++)
                        {
                            switch (Attr.AttrName)
                            {
                                case PICAAttributeName.Position: Vertices[i].Position = Vectors[i]; break;
                                case PICAAttributeName.Normal: Vertices[i].Normal = Vectors[i]; break;
                                case PICAAttributeName.Tangent: Vertices[i].Tangent = Vectors[i]; break;
                                case PICAAttributeName.TexCoord0: Vertices[i].TexCoord0 = Vectors[i]; break;
                                case PICAAttributeName.TexCoord1: Vertices[i].TexCoord1 = Vectors[i]; break;
                                case PICAAttributeName.TexCoord2: Vertices[i].TexCoord2 = Vectors[i]; break;
                                case PICAAttributeName.Color: Vertices[i].Color = Vectors[i]; break;

                                case PICAAttributeName.BoneIndex:
                                    Vertices[i].Indices[0] = (int)Vectors[i].X;
                                    Vertices[i].Indices[1] = (int)Vectors[i].Y;
                                    Vertices[i].Indices[2] = (int)Vectors[i].Z;
                                    Vertices[i].Indices[3] = (int)Vectors[i].W;
                                    break;

                                case PICAAttributeName.BoneWeight:
                                    Vertices[i].Weights[0] = Vectors[i].X;
                                    Vertices[i].Weights[1] = Vectors[i].Y;
                                    Vertices[i].Weights[2] = Vectors[i].Z;
                                    Vertices[i].Weights[3] = Vectors[i].W;
                                    break;
                            }
                        }
                    }
                    else if (VertexBuffer is GfxVertexBufferFixed)
                    {
                        //Fixed vector
                        float[] Vector = ((GfxVertexBufferFixed)VertexBuffer).Vector;

                        M.FixedAttributes.Add(new PICAFixedAttribute()
                        {
                            Name = VertexBuffer.AttrName,

                            Value = new PICAVectorFloat24(
                                Vector.Length > 0 ? Vector[0] : 0,
                                Vector.Length > 1 ? Vector[1] : 0,
                                Vector.Length > 2 ? Vector[2] : 0,
                                Vector.Length > 3 ? Vector[3] : 0)
                        });
                    }
                    else
                    {
                        //Interleaved buffer
                        GfxVertexBufferInterleaved VtxBuff = (GfxVertexBufferInterleaved)VertexBuffer;

                        foreach (GfxAttribute Attr in ((GfxVertexBufferInterleaved)VertexBuffer).Attributes)
                        {
                            M.Attributes.Add(Attr.ToPICAAttribute());
                        }

                        M.RawBuffer = VtxBuff.RawBuffer;
                        M.VertexStride = VtxBuff.VertexStride;
                    }
                }

                if (Vertices != null)
                {
                    //If any vertex buffers use one single buffer, recalculate the total stride based on all attributes setup
                    if (Shape.VertexBuffers.Any(x => x is GfxAttribute))
                        M.VertexStride = VerticesConverter.CalculateStride(M.Attributes);

                    M.RawBuffer = VerticesConverter.GetBuffer(Vertices, M.Attributes);
                }

                Vector4 PositionOffset = new Vector4(Shape.PositionOffset, 0);

                int Layer = (int)Model.Materials[Mesh.MaterialIndex].RenderLayer;

                Mesh.H3DMesh = M;
                M.MaterialIndex = (ushort)Mesh.MaterialIndex;
                M.NodeIndex = (ushort)Mesh.MeshNodeIndex;
                M.PositionOffset = PositionOffset;
                M.MeshCenter = Shape.BoundingBox.Center;
                M.Layer = Layer;
                M.Priority = Mesh.RenderPriority;

                H3DBoundingBox OBB = new H3DBoundingBox()
                {
                    Center = Shape.BoundingBox.Center,
                    Orientation = Shape.BoundingBox.Orientation,
                    Size = Shape.BoundingBox.Size
                };

                M.MetaData = new H3DMetaData();

                M.MetaData.Add(new H3DMetaDataValue(OBB));

                int SmoothCount = 0;

                foreach (GfxSubMesh SubMesh in Shape.SubMeshes)
                {
                    foreach (GfxFace Face in SubMesh.Faces)
                    {
                        foreach (GfxFaceDescriptor Desc in Face.FaceDescriptors)
                        {
                            H3DSubMesh SM = new H3DSubMesh();

                            SM.BoneIndicesCount = (ushort)SubMesh.BoneIndices.Count;

                            for (int i = 0; i < SubMesh.BoneIndices.Count; i++)
                            {
                                SM.BoneIndices[i] = (ushort)SubMesh.BoneIndices[i];
                            }

                            switch (SubMesh.Skinning)
                            {
                                case GfxSubMeshSkinning.None: SM.Skinning = H3DSubMeshSkinning.None; break;
                                case GfxSubMeshSkinning.Rigid: SM.Skinning = H3DSubMeshSkinning.Rigid; break;
                                case GfxSubMeshSkinning.Smooth: SM.Skinning = H3DSubMeshSkinning.Smooth; break;
                            }

                            SM.Indices = Desc.Indices;

                            SM.Indices = new ushort[Desc.Indices.Length];

                            Array.Copy(Desc.Indices, SM.Indices, SM.Indices.Length);

                            M.SubMeshes.Add(SM);
                        }
                    }

                    if (SubMesh.Skinning == GfxSubMeshSkinning.Smooth)
                    {
                        SmoothCount++;
                    }
                }

                if (SmoothCount == Shape.SubMeshes.Count)
                    M.Skinning = H3DMeshSkinning.Smooth;
                else if (SmoothCount > 0)
                    M.Skinning = H3DMeshSkinning.Mixed;
                else
                    M.Skinning = H3DMeshSkinning.Rigid;

                GfxMaterial Mat = Model.Materials[Mesh.MaterialIndex];

                M.UpdateBoolUniforms(Mdl.Materials[Mesh.MaterialIndex]);

                Mdl.AddMesh(M);
            }

            //Workaround to fix blending problems until I can find a proper way.
            Mdl.MeshesLayer1.Reverse();

            Mdl.MeshNodesTree = new H3DPatriciaTree();

            foreach (GfxMeshNodeVisibility MeshNode in Model.MeshNodeVisibilities)
            {
                Mdl.MeshNodesTree.Add(MeshNode.Name);
                Mdl.MeshNodesVisibility.Add(MeshNode.IsVisible);
            }

            if (Model is GfxModelSkeletal)
            {
                foreach (GfxBone Bone in ((GfxModelSkeletal)Model).Skeleton.Bones)
                {
                    H3DBone B = new H3DBone()
                    {
                        Name = Bone.Name,
                        ParentIndex = (short)Bone.ParentIndex,
                        Translation = Bone.Translation,
                        Rotation = Bone.Rotation,
                        Scale = Bone.Scale,
                        InverseTransform = Bone.InvWorldTransform
                    };

                    bool ScaleCompensate = (Bone.Flags & GfxBoneFlags.IsSegmentScaleCompensate) != 0;

                    if (ScaleCompensate) B.Flags |= H3DBoneFlags.IsSegmentScaleCompensate;

                    Mdl.Skeleton.Add(B);
                }

                Mdl.Flags |= H3DModelFlags.HasSkeleton;

                Mdl.BoneScaling = (H3DBoneScaling)((GfxModelSkeletal)Model).Skeleton.ScalingRule;
            }
            return Mdl;
        }
    }
}
