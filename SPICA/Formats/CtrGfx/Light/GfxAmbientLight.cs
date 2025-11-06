using SPICA.Math3D;

using System.Numerics;

namespace SPICA.Formats.CtrGfx.Light
{
    public class GfxAmbientLight : GfxLight
    {
        public Vector4 ColorF;

        public RGBA Color;

        private bool IsDirty;
    }
}
