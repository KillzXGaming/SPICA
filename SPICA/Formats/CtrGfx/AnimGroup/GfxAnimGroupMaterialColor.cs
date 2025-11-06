using SPICA.Formats.Common;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.AnimGroup
{
    public  class GfxAnimGroupMaterialColor : GfxAnimGroupElement
    {
        private string _MaterialName;

        public string MaterialName
        {
            get => _MaterialName;
            set => _MaterialName = value ?? throw Exceptions.GetNullException("MaterialName");
        }

        [IfVersion(CmpOp.Gequal, 0x04000000, true)] private GfxAnimGroupObjType ObjType2;

        public GfxAnimGroupMaterialColor()
        {
            ObjType = ObjType2 = GfxAnimGroupObjType.MaterialColor;
        }
    }
}
