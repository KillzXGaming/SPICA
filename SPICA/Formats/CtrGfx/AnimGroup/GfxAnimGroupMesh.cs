using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.AnimGroup
{
    public class GfxAnimGroupMesh : GfxAnimGroupElement
    {
        public int MeshIndex;

        [IfVersion(CmpOp.Gequal, 0x04000000, true)] private GfxAnimGroupObjType ObjType2;

        public GfxAnimGroupMesh()
        {
            ObjType = ObjType2 = GfxAnimGroupObjType.Mesh;
        }
    }
}
