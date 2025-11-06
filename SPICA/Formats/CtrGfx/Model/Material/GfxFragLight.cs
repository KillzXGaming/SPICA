using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Material
{
    public struct GfxFragLight
    {
        public GfxFragmentFlags Flags;

        [JsonConverter(typeof(StringEnumConverter))]
        public GfxLayerConfig LayerConfig;

        [JsonConverter(typeof(StringEnumConverter))]
        public GfxFresnelSelector FresnelSelector;

        public int BumpTexture;

        [JsonConverter(typeof(StringEnumConverter))]
        public GfxBumpMode BumpMode;

        public bool IsBumpRenormalize;

        [IfVersion(CmpOp.Equal, 0x03FFFFFF, true)] private uint HashMaybe;

        internal byte[] GetBytes()
        {
            using (MemoryStream MS = new MemoryStream())
            {
                BinaryWriter Writer = new BinaryWriter(MS);

                Writer.Write((uint)Flags);
                Writer.Write((uint)LayerConfig);
                Writer.Write((uint)FresnelSelector);
                Writer.Write(BumpTexture);
                Writer.Write((uint)BumpMode);
                Writer.Write((byte)(IsBumpRenormalize ? 1 : 0));

                return MS.ToArray();
            }
        }

        public override string ToString()
        {
            return $"{Flags} {LayerConfig} {FresnelSelector} {BumpTexture} {BumpMode} {IsBumpRenormalize}";
        }
    }
}
