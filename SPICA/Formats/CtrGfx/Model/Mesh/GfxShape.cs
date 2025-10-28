using SPICA.Formats.Common;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

using System.Collections.Generic;
using System.Numerics;

namespace SPICA.Formats.CtrGfx.Model.Mesh
{
    [TypeChoice(0x10000001u, typeof(GfxShape))]
    public class GfxShape : GfxObject
    {
        [IfVersion(CmpOp.Gequal, 0x05000000, true)]
        private uint Flags;

        public readonly GfxBoundingBox BoundingBox;

        [IfVersion(CmpOp.Less, 0x05000000, true)]
        private Vector3 BoundingCenter;

        public Vector3 PositionOffset;

        public readonly List<GfxSubMesh> SubMeshes;

        [IfVersion(CmpOp.Less, 0x05000000, true)]
        private Vector2 Pad2;

        private uint BaseAddress;

        public readonly List<GfxVertexBuffer> VertexBuffers;

        public GfxBlendShape BlendShape;

        public GfxShape()
        {
            BoundingBox = new GfxBoundingBox();

            SubMeshes = new List<GfxSubMesh>();

            VertexBuffers = new List<GfxVertexBuffer>();

            this.Header.MagicNumber = 0x4A424F53;
        }
    }
}
