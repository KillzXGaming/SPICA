using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;
using SPICA.Formats.Common;
using SPICA.PICA;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Material
{
    public struct GfxRasterization : ICustomSerialization
    {

        [Ignore] public bool IsPolygonOffsetEnabled;

        [Ignore] public GfxFaceCulling FaceCulling;

        [Ignore] public float PolygonOffsetUnit;

        [Ignore] private uint[] FaceCullingCommand;

        void ICustomSerialization.Deserialize(BinaryDeserializer Deserializer)
        {
            FaceCullingCommand = new uint[2];

            BinaryReader Reader = Deserializer.Reader;
            bool IsNintendogs = false;

            if (Deserializer.CurrentRevision <= 0x04000000)
            {
                //M-1: HACK! Nintendogs added an extra data to material struct, yet no revision fields are updated.
                //It is likely CommandCache was added to GfxMaterialColor and that's why all this is pushed forward 4 bytes.
                if (Deserializer.CurrentRevision == 0x03000000)
                {
                    long BeginPosition = Deserializer.BaseStream.Position;

                    // This is the best way to detect it... 
                    // Go to depth test function. I can't check if CullMode is valid because it can also be zero (Cull Never)
                    Reader.BaseStream.Seek(0x14, SeekOrigin.Current);

                    uint valueCheck = Reader.ReadUInt32();
                    IsNintendogs = Deserializer.IsNintendogsV3 = !Enum.IsDefined(typeof(GLTestFunc), valueCheck);

                    Reader.BaseStream.Seek(BeginPosition, SeekOrigin.Begin);
                }

                if (IsNintendogs) {
                    Reader.ReadUInt32();
                }

                IsPolygonOffsetEnabled = Reader.ReadUInt32() != 0;
                FaceCulling = ((GLFaceCulling)Reader.ReadUInt32()).ToGFX();
                uint unk0 = Reader.ReadUInt32();// Always 0?
                PolygonOffsetUnit = Reader.ReadSingle();

                if (Deserializer.CurrentRevision == 0x04000000) Reader.ReadUInt32();// Hash I assume

                if(Deserializer.CurrentRevision >= 0x04000000)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        FaceCullingCommand[i] = Reader.ReadUInt32();
                    }
                }

                if (Deserializer.CurrentRevision == 0x04000000) Reader.ReadUInt32();// Always 0?
            }
            else
            {
                IsPolygonOffsetEnabled = Reader.ReadUInt32() != 0;
                FaceCulling = (GfxFaceCulling)Reader.ReadUInt32();
                PolygonOffsetUnit = Reader.ReadSingle();

                for (int i = 0; i < 2; i++)
                {
                    FaceCullingCommand[i] = Reader.ReadUInt32();
                }
            }
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            PICACommandWriter Writer = new PICACommandWriter();

            uint CullMode = (uint)FaceCulling.ToPICAFaceCulling() & 3;

            Writer.SetCommand(PICARegister.GPUREG_FACECULLING_CONFIG, CullMode, 1);

            FaceCullingCommand = Writer.GetBuffer();

            Serializer.Writer.Write(IsPolygonOffsetEnabled ? 1u : 0u);
            Serializer.Writer.Write((uint)FaceCulling);
            Serializer.Writer.Write(PolygonOffsetUnit);

            for (int i = 0; i < 2; i++)
            {
                Serializer.Writer.Write(FaceCullingCommand[i]);
            }

            return false;
        }

        internal byte[] GetBytes()
        {
            if (FaceCullingCommand?.Length != 2)
            {
                PICACommandWriter Writer = new PICACommandWriter();
                uint CullMode = (uint)FaceCulling.ToPICAFaceCulling() & 3;
                Writer.SetCommand(PICARegister.GPUREG_FACECULLING_CONFIG, CullMode, 1);

                FaceCullingCommand = Writer.GetBuffer();
            }

            using (MemoryStream MS = new MemoryStream())
            {
                BinaryWriter Writer = new BinaryWriter(MS);

                Writer.Write(IsPolygonOffsetEnabled ? 1u : 0u);
                Writer.Write(FaceCullingCommand[0]);
                Writer.Write(FaceCullingCommand[1]);
                Writer.Write(PolygonOffsetUnit);

                return MS.ToArray();
            }
        }
    }
}
