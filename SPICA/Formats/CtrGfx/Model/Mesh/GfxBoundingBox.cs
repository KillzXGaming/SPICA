using System.Numerics;
using SPICA.Math3D;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Mesh
{
    public class GfxBoundingBox
    {
        [IfVersion(CmpOp.Greater, 0x04000000, true)] public uint Flags = 2147483648; //0x80

        public Vector3   Center;
        public Matrix3x3 Orientation;
        public Vector3   Size;
    }
}
