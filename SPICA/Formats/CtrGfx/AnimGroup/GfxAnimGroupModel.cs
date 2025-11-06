using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.AnimGroup
{
    public class GfxAnimGroupModel : GfxAnimGroupElement
    {
        [IfVersion(CmpOp.Gequal, 0x04000000, true)] private GfxAnimGroupObjType ObjType2;

        public GfxAnimGroupModel()
        {
            ObjType = ObjType2 = GfxAnimGroupObjType.Model;
        }
    }
}
