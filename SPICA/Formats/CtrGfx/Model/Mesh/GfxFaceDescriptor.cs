using SPICA.Formats.Common;
using SPICA.Math3D;
using SPICA.PICA.Commands;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;
using System.Numerics;

namespace SPICA.Formats.CtrGfx.Model.Mesh
{
    public class GfxFaceDescriptor : ICustomSerialization
    {
        public GfxGLDataType Format = GfxGLDataType.GL_UNSIGNED_SHORT;

        private byte _PrimitiveMode;

        public PICAPrimitiveMode PrimitiveMode
        {
            get => (PICAPrimitiveMode)_PrimitiveMode;
            set => _PrimitiveMode = (byte)value;
        }

        [Padding(4)] private byte Visible;

        [Section((uint)GfxSectionId.Image, Padding = 4)] private byte[] RawBuffer;

        private uint BufferObj;
        private uint LocationFlag;

        [IfVersion(CmpOp.Gequal, 0x04000000, true)] private uint CommandCachePtr;
        [IfVersion(CmpOp.Gequal, 0x04000000, true)] private uint CommandCacheLength;

        [IfVersion(CmpOp.Gequal, 0x04000000, true)] private uint LocationPtr;
        [IfVersion(CmpOp.Gequal, 0x04000000, true)] private uint MemoryArea;

        [IfVersion(CmpOp.Less,   0x04000000, true)] private uint Unk;
        [IfVersion(CmpOp.Gequal, 0x05000000, true)] public uint BoundingVolume;

        [Ignore] public ushort[] Indices;

        void ICustomSerialization.Deserialize(BinaryDeserializer Deserializer)
        {
            bool IsBuffer16Bits = Format == GfxGLDataType.GL_UNSIGNED_SHORT;

            Indices = new ushort[RawBuffer.Length >> (IsBuffer16Bits ? 1 : 0)];

            for (int i = 0; i < RawBuffer.Length; i += (IsBuffer16Bits ? 2 : 1))
            {
                if (IsBuffer16Bits)
                {
                    Indices[i >> 1] = (ushort)(
                        RawBuffer[i + 0] << 0 |
                        RawBuffer[i + 1] << 8);
                }
                else
                {
                    Indices[i] = RawBuffer[i];
                }
            }
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            bool IsBuffer16Bits = Format == GfxGLDataType.GL_UNSIGNED_SHORT;

            //TODO
            var mem = new System.IO.MemoryStream();
            using (var writer = new System.IO.BinaryWriter(mem))
            {
                for (int i = 0; i < Indices.Length; i++)
                {
                    if (IsBuffer16Bits)
                        writer.Write(Indices[i]);
                    else
                        writer.Write((byte)Indices[i]);
                }
            }
            RawBuffer = mem.ToArray();
            return false;
        }

        public GfxFaceDescriptor()
        {
            Visible = 1;
        }
    }
}
