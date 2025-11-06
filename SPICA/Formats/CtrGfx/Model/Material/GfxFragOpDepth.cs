using SPICA.Formats.Common;
using SPICA.Formats.Generic.StudioMdl;
using SPICA.PICA;
using SPICA.PICA.Commands;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Material
{
    public struct GfxFragOpDepth : ICustomSerialization
    {
        public GfxFragOpDepthFlags Flags;

        [Inline, FixedLength(4), IfVersion(CmpOp.Greater, 0x03000000)] private uint[] Commands;

        [Ignore] public PICADepthColorMask ColorMask;

        void ICustomSerialization.Deserialize(BinaryDeserializer Deserializer)
        {
            if(Deserializer.CurrentRevision < 0x04000000)
            {
                ColorMask = new PICADepthColorMask();
                BinaryReader Reader = Deserializer.Reader;

                ColorMask.DepthFunc = ((GLTestFunc)Reader.ReadUInt32()).ToPICA();
                var unk0 = Reader.ReadUInt32();
                ColorMask.Enabled = (Flags & GfxFragOpDepthFlags.IsTestEnabled) > 0;
                ColorMask.DepthWrite = (Flags & GfxFragOpDepthFlags.IsMaskEnabled) > 0;
            }
            else
            {

                PICACommandReader CmdReader = new PICACommandReader(Commands);

                while (CmdReader.HasCommand)
                {
                    PICACommand Cmd = CmdReader.GetCommand();

                    if (Cmd.Register == PICARegister.GPUREG_DEPTH_COLOR_MASK)
                    {
                        ColorMask = new PICADepthColorMask(Cmd.Parameters[0]);
                    }
                }

                //Is this right for v5? Was it wrong before?
                //if(Deserializer.MainFileVersion == 0x03FFFFFF)
                {
                    ColorMask.DepthWrite = (Flags & GfxFragOpDepthFlags.IsMaskEnabled) > 0;
                }
            }
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            PICACommandWriter Writer = new PICACommandWriter();

            uint ZDepth;

            if ((uint)ColorMask.DepthFunc > 1)
                ZDepth = (uint)ColorMask.DepthFunc > 5 ? 2u : 3u;
            else
                ZDepth = (uint)ColorMask.DepthFunc;

            Writer.SetCommand(PICARegister.GPUREG_DEPTH_COLOR_MASK, ColorMask.ToUInt32(), 1);

            Writer.SetCommand(PICARegister.GPUREG_GAS_DELTAZ_DEPTH, ZDepth << 24, 8);

            Commands = Writer.GetBuffer();

            return false;
        }
    }
}
