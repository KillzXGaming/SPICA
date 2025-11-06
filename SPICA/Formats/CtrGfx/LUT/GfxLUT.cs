using SPICA.Formats.CtrH3D.LUT;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.LUT
{
    [TypeChoice(0x04000000u, typeof(GfxLUT))]
    [TypeChoice(0x00000020u, typeof(GfxLUT))]
    public class GfxLUT : GfxObject
    {
        public override GfxObjRevisionsV5 Revision => GfxObjRevisionsV5.LUT;

        public readonly GfxDict<GfxLUTSampler> Samplers;

        public GfxLUT()
        {
            //Header.MagicNumber = 0x5354554C;
            Samplers = new GfxDict<GfxLUTSampler>();
        }

        public static GfxLUT FromH3D(H3DLUT lut)
        {
            GfxLUT gfxLut = new GfxLUT();
            gfxLut.Name = lut.Name;
            foreach (var sampler in lut.Samplers)
            {
                gfxLut.Samplers.Add(new GfxLUTSampler()
                {
                    Table = sampler.Table,
                    IsAbsolute = sampler.Flags.HasFlag(H3DLUTFlags.IsAbsolute),
                    Name = sampler.Name,
                });
            }
            return gfxLut;
        }
    }
}
