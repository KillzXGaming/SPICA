using System.Numerics;
using SPICA.Math3D;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Light
{
    public class GfxFragmentLight : GfxLight
    {
        public GfxLightType Type;

        public Vector4 AmbientColorF;
        public Vector4 DiffuseColorF;
        public Vector4 Specular0ColorF;
        public Vector4 Specular1ColorF;

        public RGBA AmbientColor;
        public RGBA DiffuseColor;
        public RGBA Specular0Color;
        public RGBA Specular1Color;

        public Vector3 Direction;

        [IfVersion(CmpOp.Greater, 0x04000000)] public GfxLUTReference DistanceSampler;
        [IfVersion(CmpOp.Greater, 0x04000000)] public GfxFragLightLUT AngleSampler;

        public float AttenuationStart;
        public float AttenuationEnd;

        public uint InvAttScaleF20;
        public uint AttBiasF20;

        public GfxFragmentLightFlags Flags;
    }
}
