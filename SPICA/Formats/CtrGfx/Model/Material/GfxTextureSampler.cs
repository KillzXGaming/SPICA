using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Material
{
    //(M-1)TODO: Some old steel diver model use Old but have type set to 0x80000000.... UGH! I hate SPICA
    [TypeChoice(0x80000000u, typeof(GfxTextureSamplerStd))]
    [TypeChoice(0x00000001u, typeof(GfxTextureSamplerOld))]
    public class GfxTextureSampler
    {
        [IfVersion(CmpOp.Greater, 0x04000000)]
        public GfxTextureMapper Parent;

        [IfVersion(CmpOp.Greater, 0x04000000)]
        public GfxTextureMinFilter MinFilter;
    }
}
