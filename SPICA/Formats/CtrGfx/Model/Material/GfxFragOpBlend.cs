using System;
using System.Drawing.Imaging;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SPICA.Formats.Common;
using SPICA.Math3D;
using SPICA.PICA;
using SPICA.PICA.Commands;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Material
{
    public struct GfxFragOpBlend : ICustomSerialization
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public GfxFragOpBlendMode Mode;

        private Vector4 ColorF;

        [Inline, FixedLength(6), IfVersion(CmpOp.Greater, 0x03000000)] private uint[] Commands;

        [Ignore] public PICAColorOperation ColorOperation;

        [Ignore] public PICABlendFunction Function;

        [Ignore] public PICALogicalOp LogicalOperation;

        [Ignore] public RGBA Color;

        void ICustomSerialization.Deserialize(BinaryDeserializer Deserializer)
        {
            if (Deserializer.CurrentRevision < 0x04000000)
            {
                ColorOperation = new PICAColorOperation();
                Function = new PICABlendFunction();
                Color = RGBA.FromFloat(ColorF);

                BinaryReader Reader = Deserializer.Reader;

                LogicalOperation = ((GLLogicOp)Reader.ReadUInt32()).ToPICA();
                Function.ColorSrcFunc = ((GLBlendFunc)Reader.ReadUInt32()).ToPICA();
                Function.ColorDstFunc = ((GLBlendFunc)Reader.ReadUInt32()).ToPICA();
                Function.ColorEquation = ((GLBlendEquation)Reader.ReadUInt32()).ToPICA();
                Function.AlphaSrcFunc = ((GLBlendFunc)Reader.ReadUInt32()).ToPICA();
                Function.AlphaDstFunc = ((GLBlendFunc)Reader.ReadUInt32()).ToPICA();
                Function.AlphaEquation = ((GLBlendEquation)Reader.ReadUInt32()).ToPICA();

                ColorOperation.BlendMode = Mode == GfxFragOpBlendMode.LogicalOp ? PICABlendMode.LogicalOp : PICABlendMode.Blend;
            }
            else
            {


                PICACommandReader CmdReader = new PICACommandReader(Commands);

                while (CmdReader.HasCommand)
                {
                    PICACommand Cmd = CmdReader.GetCommand();

                    uint Param = Cmd.Parameters[0];

                    switch (Cmd.Register)
                    {
                        case PICARegister.GPUREG_COLOR_OPERATION: ColorOperation = new PICAColorOperation(Param); break;

                        case PICARegister.GPUREG_BLEND_FUNC: Function = new PICABlendFunction(Param); break;

                        case PICARegister.GPUREG_LOGIC_OP: LogicalOperation = (PICALogicalOp)(Param & 0xf); break;

                        case PICARegister.GPUREG_BLEND_COLOR: Color = new RGBA(Param); break;
                    }
                }
            }
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            PICACommandWriter Writer = new PICACommandWriter();

            uint LogicalOp = (uint)LogicalOperation & 0xf;

            Writer.SetCommand(PICARegister.GPUREG_COLOR_OPERATION, true,
                ColorOperation.ToUInt32(),
                Function.ToUInt32(),
                LogicalOp,
                Color.ToUInt32());

            Commands = new uint[6];

            Buffer.BlockCopy(Writer.GetBuffer(), 0, Commands, 0, 5);

            Commands = Writer.GetBuffer();

            ColorF = Color.ToVector4();

            return false;
        }
    }
}
