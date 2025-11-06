using System.Numerics;
using System.Runtime.Intrinsics;
using SPICA.Formats.Common;
using SPICA.Math3D;
using SPICA.PICA;
using SPICA.PICA.Commands;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Material
{
    public class GfxFragShader : ICustomSerialization
    {
        [IfVersion(CmpOp.Greater, 0x3000000)] private Vector4 TexEnvBufferColorF;

        [IfVersion(CmpOp.Greater, 0x3000000)] public GfxFragLight     Lighting;
        [IfVersion(CmpOp.Greater, 0x3000000)] public GfxFragLightLUTs LUTs;

        [Inline, FixedLength(6), IfVersion(CmpOp.Greater, 0x3000000)]
        public GfxTexEnv[] TextureEnvironments;

        [IfVersion(CmpOp.Greater, 0x3000000)] public GfxAlphaTest AlphaTest;

        [Inline, FixedLength(6), IfVersion(CmpOp.Greater, 0x3000000)]
        private uint[] Commands;

        [Ignore] public RGBA TexEnvBufferColor;

        public GfxFragShader()
        {
            LUTs = new GfxFragLightLUTs();

            TextureEnvironments = new GfxTexEnv[6];
        }

        void ICustomSerialization.Deserialize(BinaryDeserializer Deserializer)
        {
            if (Deserializer.CurrentRevision <= 0x3000000)
            {
                Lighting = new GfxFragLight();
                AlphaTest = new GfxAlphaTest() { Test = new PICAAlphaTest() };

                BinaryReader Reader = Deserializer.Reader;

                Lighting.LayerConfig = ((GLLayerConfig)Reader.ReadUInt32()).ToGFX();
                TexEnvBufferColorF = Reader.ReadVector4();
                Lighting.BumpTexture = ((GLBumpTexture)Reader.ReadUInt32()).ToInt();
                Lighting.BumpMode = ((GLBumpMode)Reader.ReadUInt32()).ToPICA();
                Lighting.IsBumpRenormalize = Reader.ReadUInt32() != 0;
                uint LUTSamplerCount = Reader.ReadUInt32();//Idk yet
                Lighting.FresnelSelector = ((GLFresnelSelector)Reader.ReadUInt32()).ToPICA();

                uint FragmentLightingTableOffs;
                if (Deserializer.CurrentRevision <= 0x2000000)
                {
                    //M-1: The hell is this? Was just for testing LUTs?
                    GLLUTInput Input = (GLLUTInput)Reader.ReadUInt32();
                    float Scale = Reader.ReadSingle();

                    
                    FragmentLightingTableOffs = Deserializer.ReadPointer();
                }
                else
                {
                    //(M-1)TODO: Does any v3 model use LUTs? 
                    FragmentLightingTableOffs = Deserializer.ReadPointer();
                }

                for (int i = 0; i < 6; i++)
                {
                    var texEnv = TextureEnvironments[i] = new GfxTexEnv();
                    var texEnvStage = TextureEnvironments[i].Stage;

                    texEnvStage.Combiner.Color = ((GLTexCombinerMode)Reader.ReadUInt32()).ToPICA();
                    texEnvStage.Combiner.Alpha = ((GLTexCombinerMode)Reader.ReadUInt32()).ToPICA();
                    texEnvStage.Scale.Color = GetScalePICA(Reader.ReadSingle());
                    texEnvStage.Scale.Alpha = GetScalePICA(Reader.ReadSingle());

                    texEnv.Constant = (GfxTexEnvConstant)Reader.ReadUInt32();
                    //Color
                    texEnvStage.Source.Color[0] = ((GLTexCombinerSrc)Reader.ReadUInt32()).ToPICA();
                    texEnvStage.Source.Color[1] = ((GLTexCombinerSrc)Reader.ReadUInt32()).ToPICA();
                    texEnvStage.Source.Color[2] = ((GLTexCombinerSrc)Reader.ReadUInt32()).ToPICA();
                    //Color
                    texEnvStage.Operand.Color[0] = ((GLTexCombineOp)Reader.ReadUInt32()).ToPICA_Color();
                    texEnvStage.Operand.Color[1] = ((GLTexCombineOp)Reader.ReadUInt32()).ToPICA_Color();
                    texEnvStage.Operand.Color[2] = ((GLTexCombineOp)Reader.ReadUInt32()).ToPICA_Color();
                    //Alpha
                    texEnvStage.Source.Alpha[0] = ((GLTexCombinerSrc)Reader.ReadUInt32()).ToPICA();
                    texEnvStage.Source.Alpha[1] = ((GLTexCombinerSrc)Reader.ReadUInt32()).ToPICA();
                    texEnvStage.Source.Alpha[2] = ((GLTexCombinerSrc)Reader.ReadUInt32()).ToPICA();
                    //Alpha
                    texEnvStage.Operand.Alpha[0] = ((GLTexCombineOp)Reader.ReadUInt32()).ToPICA_Alpha();
                    texEnvStage.Operand.Alpha[1] = ((GLTexCombineOp)Reader.ReadUInt32()).ToPICA_Alpha();
                    texEnvStage.Operand.Alpha[2] = ((GLTexCombineOp)Reader.ReadUInt32()).ToPICA_Alpha();

                    //(M-1) TODO: Make sure this is accurate to hardware
                    texEnvStage.UpdateColorBuffer = ((GLTexCombinerSrc)Reader.ReadUInt32()) == GLTexCombinerSrc.Previous;
                    texEnvStage.UpdateAlphaBuffer = ((GLTexCombinerSrc)Reader.ReadUInt32()) == GLTexCombinerSrc.Previous;

                    if (Deserializer.IsNintendogsV3)
                    {
                        ushort SrcRgb = Reader.ReadUInt16();
                        ushort SrcAlpha = Reader.ReadUInt16();
                        uint Address = Reader.ReadUInt32();
                        uint Operands = Reader.ReadUInt32();
                        ushort CombineRgb = Reader.ReadUInt16();
                        ushort CombineAlpha = Reader.ReadUInt16();
                        RGBA color = new RGBA(Reader.ReadUInt32());
                        ushort ScaleRgb = Reader.ReadUInt16();
                        ushort ScaleAlpha = Reader.ReadUInt16();
                    }
                }

                AlphaTest.Test.Enabled = Reader.ReadUInt32() != 0;
                AlphaTest.Test.Function = ((GLTestFunc)Reader.ReadUInt32()).ToPICA();
                AlphaTest.Test.Reference = (byte)(Reader.ReadSingle() * 255);

                if (Deserializer.CurrentRevision > 0x2000000)
                {
                    Commands = new uint[6];
                    for (int i = 0; i < 6; i++)
                    {
                        Commands[i] = Reader.ReadUInt32();
                    }
                }
            }

            if (Deserializer.CurrentRevision > 0x2000000)
            {

                PICACommandReader Reader = new PICACommandReader(Commands);

                while (Reader.HasCommand)
                {
                    PICACommand Cmd = Reader.GetCommand();

                    uint Param = Cmd.Parameters[0];

                    switch (Cmd.Register)
                    {
                        case PICARegister.GPUREG_TEXENV_UPDATE_BUFFER:
                            TextureEnvironments[1].Stage.UpdateColorBuffer = (Param & 0x100) != 0;
                            TextureEnvironments[2].Stage.UpdateColorBuffer = (Param & 0x200) != 0;
                            TextureEnvironments[3].Stage.UpdateColorBuffer = (Param & 0x400) != 0;
                            TextureEnvironments[4].Stage.UpdateColorBuffer = (Param & 0x800) != 0;

                            TextureEnvironments[1].Stage.UpdateAlphaBuffer = (Param & 0x1000) != 0;
                            TextureEnvironments[2].Stage.UpdateAlphaBuffer = (Param & 0x2000) != 0;
                            TextureEnvironments[3].Stage.UpdateAlphaBuffer = (Param & 0x4000) != 0;
                            TextureEnvironments[4].Stage.UpdateAlphaBuffer = (Param & 0x8000) != 0;
                            break;

                        case PICARegister.GPUREG_TEXENV_BUFFER_COLOR: TexEnvBufferColor = new RGBA(Param); break;
                    }
                }
            }
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            PICACommandWriter Writer = new PICACommandWriter();

            uint UpdateColor = 0;

            if (TextureEnvironments[1].Stage.UpdateColorBuffer) UpdateColor |= 0x100;
            if (TextureEnvironments[2].Stage.UpdateColorBuffer) UpdateColor |= 0x200;
            if (TextureEnvironments[3].Stage.UpdateColorBuffer) UpdateColor |= 0x400;
            if (TextureEnvironments[4].Stage.UpdateColorBuffer) UpdateColor |= 0x800;

            if (TextureEnvironments[1].Stage.UpdateAlphaBuffer) UpdateColor |= 0x1000;
            if (TextureEnvironments[2].Stage.UpdateAlphaBuffer) UpdateColor |= 0x2000;
            if (TextureEnvironments[3].Stage.UpdateAlphaBuffer) UpdateColor |= 0x4000;
            if (TextureEnvironments[4].Stage.UpdateAlphaBuffer) UpdateColor |= 0x8000;

            Writer.SetCommand(PICARegister.GPUREG_TEXENV_BUFFER_COLOR, TexEnvBufferColor.ToUInt32());

            Writer.SetCommand(PICARegister.GPUREG_TEXENV_UPDATE_BUFFER, UpdateColor, 2);

            Writer.SetCommand(PICARegister.GPUREG_LIGHTING_CONFIG0, 0x400u, 2);

            Commands = Writer.GetBuffer();

            TexEnvBufferColorF = TexEnvBufferColor.ToVector4();

            return false;
        }

        private PICATextureCombinerScale GetScalePICA(float scale)
        {
            switch (scale)
            {
                case 1f: return PICATextureCombinerScale.One;
                case 2f: return PICATextureCombinerScale.Two;
                case 4f: return PICATextureCombinerScale.Four;
                default: throw new ArgumentException($"Invalid Combiner Scale {scale}!");
            }
        }
    }
}
