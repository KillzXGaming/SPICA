using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Mesh
{
    [TypeChoice(0x01000000u, typeof(GfxMesh))]
    public class GfxMesh : GfxObject
    {
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
         */
        private uint Flags;

        [Inline, FixedLength(12)] private uint[] AttrScaleCommands;

        private uint EnableCommandsPtr;
        private uint EnableCommandsLength;

        private uint DisableCommandsPtr;
        private uint DisableCommandsLength;

        private string _MeshNodeName;

        public string MeshNodeName
        {
            get => _MeshNodeName;
            set => _MeshNodeName = value ?? throw Exceptions.GetNullException("MeshNodeName");
        }

        private uint RenderKeyCache;
        private uint  CommandAlloc;
        private uint Unk1;
        private uint Unk2;

        public GfxMesh()
        {
            AttrScaleCommands = new uint[12];

            this.Header.MagicNumber = 0x4A424F53;
        }
    }
}
