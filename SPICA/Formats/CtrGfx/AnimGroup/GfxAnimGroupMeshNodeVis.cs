using SPICA.Formats.Common;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.AnimGroup
{
    public class GfxAnimGroupMeshNodeVis : GfxAnimGroupElement
    {
        private string _NodeName;

        public string NodeName
        {
            get => _NodeName;
            set => _NodeName = value ?? throw Exceptions.GetNullException("NodeName");
        }

        [IfVersion(CmpOp.Gequal, 0x04000000, true)] private GfxAnimGroupObjType ObjType2;

        public GfxAnimGroupMeshNodeVis()
        {
            ObjType = ObjType2 = GfxAnimGroupObjType.MeshNodeVisibility;
        }
    }
}
