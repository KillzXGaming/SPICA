using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Mesh
{
    [TypeChoice(0x01000000u, typeof(GfxMesh))]
    [TypeChoice(0x00000080u, typeof(GfxMesh))]
    public class GfxMesh : GfxObject
    {
        public override GfxObjRevisionsV5 Revision => GfxObjRevisionsV5.Mesh;

        [Ignore]
        [Newtonsoft.Json.JsonIgnore]
        public H3DMesh H3DMesh;

        public int ShapeIndex;
        public int MaterialIndex;

        [Newtonsoft.Json.JsonIgnore]
        public GfxModel Parent;

        private byte Visible;

        public bool IsVisible
        {
            get => Visible != 0;
            set => Visible = (byte)(value ? 1 : 0);
        }

        public byte RenderPriority;

        public short MeshNodeIndex;

        public int PrimitiveIndex;

        /*
         * Stuff below is filled by game engine with data (see H3DMesh for meaning of those).
         * On the binary model file it's always zero so we can just ignore.
         * 
         * We have to compare to CGFX version because v4 changed from v5, yet mesh revision is still 1.0.0.0
         */
        [IfVersion(CmpOp.Gequal, 0x04000000, true)]
        private uint Flags;

        [IfVersion(CmpOp.Gequal, 0x04000000, true)]
        [Inline, FixedLength(12)] private uint[] AttrScaleCommands;

        [IfVersion(CmpOp.Gequal, 0x04000000, true)]
        private uint EnableCommandsPtr;
        [IfVersion(CmpOp.Gequal, 0x04000000, true)]
        private uint EnableCommandsLength;

        [IfVersion(CmpOp.Gequal, 0x05000000, true)]
        private uint DisableCommandsPtr;
        [IfVersion(CmpOp.Gequal, 0x05000000, true)]
        private uint DisableCommandsLength;

        [IfVersion(CmpOp.Gequal, 0x04000000, true)]
        private string _MeshNodeName = "";

        public string MeshNodeName
        {
            get => _MeshNodeName;
            set => _MeshNodeName = value ?? throw Exceptions.GetNullException("MeshNodeName");
        }

        [IfVersion(CmpOp.Gequal, 0x04000000, true)]
        private uint RenderKeyCache;
        [IfVersion(CmpOp.Gequal, 0x04000000, true)]
        private uint  CommandAlloc;
        [IfVersion(CmpOp.Gequal, 0x04000000, true)]
        private uint Unk1;
        [IfVersion(CmpOp.Gequal, 0x05000000, true)]
        private uint Unk2;

        public GfxMesh()
        {
            AttrScaleCommands = new uint[12];

            this.Header.MagicNumber = 0x4A424F53;
        }
    }
}
