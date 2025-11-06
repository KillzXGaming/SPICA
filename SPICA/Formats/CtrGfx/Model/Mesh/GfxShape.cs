using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using SPICA.Formats.Common;
using SPICA.Formats.CtrGfx.AnimGroup;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Mesh
{
    [TypeChoice(0x10000001u, typeof(GfxShape))]
    [TypeChoice(0x00000108u, typeof(GfxShape))]
    public class GfxShape : GfxObject, ICustomSerialization
    {
        public override GfxObjRevisionsV5 Revision => GfxObjRevisionsV5.Shape;

        private uint Flags;

        [IfVersion(CmpOp.Gequal, 0x05000000, true)] public readonly GfxBoundingBox BoundingBox;

        [IfVersion(CmpOp.Equal,  0x04000000, true), Inline] public readonly GfxBoundingBox BoundingBoxV4;

        [IfVersion(CmpOp.Lequal, 0x03FFFFFF, true)] public Vector3 MeshCenter;

        public Vector3 PositionOffset;

        public readonly List<GfxSubMesh> SubMeshes;

        [IfVersion(CmpOp.Greater, 0x03000000, true)] private uint BaseAddress;

        [IfVersion(CmpOp.Equal, 0x03FFFFFF, true)] internal Vector2 Unk0;// Always 0?

        public readonly List<GfxVertexBuffer> VertexBuffers;

        public GfxBlendShape BlendShape;

        public GfxShape()
        {
            BoundingBox = new GfxBoundingBox();

            SubMeshes = new List<GfxSubMesh>();

            VertexBuffers = new List<GfxVertexBuffer>();

            BlendShape = new GfxBlendShape();

            this.Header.MagicNumber = 0x4A424F53;
        }

        public void Deserialize(BinaryDeserializer Deserializer)
        {
            if(BoundingBoxV4 != null)
            {
                BoundingBox.Center = BoundingBoxV4.Center;
                BoundingBox.Orientation = BoundingBoxV4.Orientation;
                BoundingBox.Size = BoundingBoxV4.Size;
            }
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            return false;
        }
    }
}
