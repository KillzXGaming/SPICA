using System.IO;
using SPICA.Formats.Common;
using SPICA.PICA;
using SPICA.PICA.Commands;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Material
{
    public struct GfxFragOpStencil : ICustomSerialization
    {
        //TODO: Fix
        [Inline, FixedLength(4), IfVersion(CmpOp.Greater, 0x04000000)] private uint[] Commands;

        [Ignore] public PICAStencilTest Test;

        [Ignore] public PICAStencilOperation Operation;

        void ICustomSerialization.Deserialize(BinaryDeserializer Deserializer)
        {
            //M-1: Are one of these BufferMask?
            if (Deserializer.CurrentRevision < 0x05000000)
            {
                Test = new PICAStencilTest();
                Operation = new PICAStencilOperation();
                Commands = new uint[4];

                BinaryReader Reader = Deserializer.Reader;

                uint Unk0 = Reader.ReadUInt32();
                Test.Enabled = Reader.ReadUInt32() != 0;
                Test.Function = ((GLTestFunc)Reader.ReadUInt32()).ToPICA();
                Test.Reference = (byte)(Reader.ReadSingle() * byte.MaxValue);
                Test.Mask = (byte)Reader.ReadUInt32();

                Operation.FailOp = ((GLStencilOp)Reader.ReadUInt32()).ToPICA();
                Operation.ZFailOp = ((GLStencilOp)Reader.ReadUInt32()).ToPICA();
                Operation.ZPassOp = ((GLStencilOp)Reader.ReadUInt32()).ToPICA();

                if(Deserializer.CurrentRevision > 0x03000000)
                {
                    var hashMaybeA = Reader.ReadUInt32();

                    for (int i = 0; i < Commands.Length; i++)
                    {
                        Commands[i] = Reader.ReadUInt32();
                    }

                    var hashMaybeB = Reader.ReadUInt32();
                }
            }

            PICACommandReader CmdReader = new PICACommandReader(Commands);

            while (CmdReader.HasCommand)
            {
                PICACommand Cmd = CmdReader.GetCommand();

                uint Param = Cmd.Parameters[0];

                switch (Cmd.Register)
                {
                    case PICARegister.GPUREG_STENCIL_TEST: Test = new PICAStencilTest(Param); break;

                    case PICARegister.GPUREG_STENCIL_OP: Operation = new PICAStencilOperation(Param); break;
                }
            }
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            RebuildCommands();

            return false;
        }

        internal byte[] GetBytes()
        {
            using (MemoryStream MS = new MemoryStream())
            {
                BinaryWriter Writer = new BinaryWriter(MS);

                RebuildCommands();

                foreach (uint Cmd in Commands)
                {
                    Writer.Write(Cmd);
                }

                return MS.ToArray();
            }
        }

        private void RebuildCommands()
        {
            PICACommandWriter Writer = new PICACommandWriter();

            Writer.SetCommand(PICARegister.GPUREG_STENCIL_TEST, Test.ToUInt32(), 13);

            Writer.SetCommand(PICARegister.GPUREG_STENCIL_OP, Operation.ToUInt32());

            Commands = Writer.GetBuffer();
        }
    }
}
