using SPICA.Formats.Common;
using SPICA.Formats.CtrGfx.LUT;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx
{
    [TypeChoice(0x40000000u, typeof(GfxLUTReference))]
    [TypeChoice(0x00000002u, typeof(GfxLUTReference))]
    public class GfxLUTReference
    {
        private string _TableName;

        public string TableName
        {
            get => _TableName;
            set => _TableName = value ?? throw Exceptions.GetNullException("TableName");
        }

        private string _SamplerName;

        public string SamplerName
        {
            get => _SamplerName;
            set => _SamplerName = value ?? throw Exceptions.GetNullException("SamplerName");
        }

        public GfxLUT TargetLUT;
    }
}
