using Newtonsoft.Json.Linq;
using SPICA.Formats.Common;
using SPICA.Math3D;
using SPICA.PICA.Commands;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;
using SPICA.Serialization.Serializer;

namespace SPICA.Formats.CtrGfx.Texture
{
    public class GfxTextureImage : GfxTexture, ICustomSerialization
    {
        [Ignore]
        public GfxTextureImageData Image;

        public void Deserialize(BinaryDeserializer Deserializer)
        {
            var pos = Deserializer.BaseStream.Position;

            Deserializer.BaseStream.Seek(Deserializer.ReadPointer(), SeekOrigin.Begin);
            Image = Deserializer.Deserialize<GfxTextureImageData>();

            if (Deserializer.MainFileVersion < 0x04000000)
            {
                HwFormat = ((GLTexFormat)((GLType << 16) | GLFormat)).ToPICA();
            }

            Deserializer.BaseStream.Seek(pos, SeekOrigin.Begin);
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            //Write out the data directly instead
            Serializer.WriteValue(Header);

            Serializer.AddReference(typeof(string), new RefValue()
            {
                Value = this.Name,
                Position = Serializer.BaseStream.Position,
            });
            Serializer.Writer.Write(0);

            Serializer.WriteValue(MetaData);

            Serializer.Writer.Write(Height);
            Serializer.Writer.Write(Width);
            Serializer.Writer.Write(GLFormat);
            Serializer.Writer.Write(GLType);
            Serializer.Writer.Write(MipmapSize);
            Serializer.Writer.Write(TextureObj);
            Serializer.Writer.Write(LocationFlag);
            Serializer.Writer.Write((uint)HwFormat);

            Serializer.Writer.Write(4);
            Serializer.WriteValue(Image);

            return true;
        }
    }
}
