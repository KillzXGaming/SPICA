using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp.PixelFormats;
using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.Math3D;
using SPICA.PICA.Commands;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Material
{
    internal class GfxTextureSamplerOld : GfxTextureSampler, ICustomSerialization
    {
        public new GLTexMinFilter MinFilter;
        public GLTexMagFilter MagFilter;
        public Vector4 BorderColorF;
        public GLTexWrap WrapS;
        public GLTexWrap WrapT;
        public float MinLod;
        public float LodBias;

        [IfVersion(CmpOp.Greater, 0x03000000, true)] public RGBA BorderColor;
        [IfVersion(CmpOp.Greater, 0x03000000, true)] private uint Hash;

        public void Deserialize(BinaryDeserializer Deserializer)
        {
            if(Deserializer.MainFileVersion > 0x03000000)
            {
                BorderColor = new RGBA(Deserializer.Reader.ReadUInt32());
                Hash = Deserializer.Reader.ReadUInt32();
            }
            else
            {
                BorderColor = RGBA.FromFloat(BorderColorF);
            }
        }

        public bool Serialize(BinarySerializer Serializer)
        {
            return false;
        }
    }
}
