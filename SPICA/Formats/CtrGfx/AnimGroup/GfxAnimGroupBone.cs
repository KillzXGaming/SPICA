using SPICA.Formats.Common;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.AnimGroup
{
    public class GfxAnimGroupBone : GfxAnimGroupElement
    {
        private string _BoneName;

        public string BoneName
        {
            get => _BoneName;
            set => _BoneName = value ?? throw Exceptions.GetNullException("BoneName");
        }

        [IfVersion(CmpOp.Gequal, 0x04000000, true)] private GfxAnimGroupObjType ObjType2;

        public GfxAnimGroupBone()
        {
            ObjType = ObjType2 = GfxAnimGroupObjType.Bone;
        }
    }
}
