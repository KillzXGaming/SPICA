using Newtonsoft.Json;
using SPICA.Formats.Common;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrGfx.Model.Material;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Math3D;
using SPICA.PICA;
using SPICA.PICA.Commands;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;
using SPICA.Serialization.Serializer;

using System;
using System.Linq;
using System.Numerics;

namespace SPICA.Formats.CtrH3D.Model.Material
{
    [Inline]
    public class H3DMaterial : ICustomSerialization, INamed
    {
        public H3DMaterialParams MaterialParams;

        [Ignore]
        public PokemonSettings PokemonUserBooleans = new PokemonSettings();

        //Settings specifically configured by user boolean options for vertex data
        public class PokemonSettings
        {
            public bool IsLightEnabled;
            public bool IsRimEnabled;
            public bool IsPhongEnabled;
            public bool IsInverseLightEnabled;
        }

        [Ignore]
        public List<GfxShaderParam> BcresShaderParams = new List<GfxShaderParam>();

        [Ignore]
        public GfxFragOpDepthFlags BcresDepthFlags = GfxFragOpDepthFlags.IsTestEnabled | GfxFragOpDepthFlags.IsMaskEnabled;

        [Ignore]
        public GfxDict<GfxMetaData> BcresUserData = new GfxDict<GfxMetaData>();

        [Ignore]
        [JsonIgnore]
        public EventHandler UpdateShaders;
        [Ignore]
        [JsonIgnore]
        public EventHandler UpdateShaderUniforms;
        [Ignore]
        [JsonIgnore]
        public string FragmentShader;

        public H3DTexture Texture0;
        public H3DTexture Texture1;
        public H3DTexture Texture2;

        private uint[] TextureCommands;

        [FixedLength(3), IfVersion(CmpOp.Gequal, 0x21)] public H3DTextureMapper[] TextureMappers;

        /*
         * Older BCH versions had the Texture Mappers stored directly within the Material data.
         * Newer versions (see above) uses a pointer and stores it somewhere else instead.
         */
        [FixedLength(3), IfVersion(CmpOp.Less, 0x21), Inline] private H3DTextureMapper[] TextureMappersCompat;

        public string Texture0Name;
        public string Texture1Name;
        public string Texture2Name;

        private string _Name;

        public string Name
        {
            get => _Name;
            set => _Name = value ?? throw Exceptions.GetNullException("Name");
        }

        [Ignore] public bool[] EnabledTextures;

        [Ignore]
        [JsonIgnore]
        public RenderPreset RenderingPreset
        {
            get
            {
                if (this.MaterialParams.AlphaTest.Enabled)
                    return RenderPreset.Transparent;
                else if (this.MaterialParams.BlendFunction.ColorSrcFunc == PICABlendFunc.SourceAlpha &&
                         this.MaterialParams.BlendFunction.ColorDstFunc == PICABlendFunc.OneMinusSourceAlpha &&
                         this.MaterialParams.BlendFunction.AlphaSrcFunc == PICABlendFunc.SourceAlpha &&
                         this.MaterialParams.BlendFunction.AlphaDstFunc == PICABlendFunc.OneMinusSourceAlpha)
                    return RenderPreset.Translucent;
                else
                    return RenderPreset.Opaque;
            }
            set
            {
                if (value == RenderPreset.Opaque) SetOpaque();
                else if (value == RenderPreset.Translucent) SetTranslucent();
                else if (value == RenderPreset.Transparent) SetTransparent();
                else if (value == RenderPreset.Custom) SetCustom();
                else
                    throw new System.Exception($"Unsupported preset mode {value}!");
            }
        }

        public enum RenderPreset
        {
            Opaque,
            Transparent,
            Translucent,
            Custom,
        }

        public static H3DMaterial GetSimpleMaterial(
            string ModelName,
            string MaterialName,
            string TextureName,
            string ShaderName = "DefaultShader",
            int    ShaderIndex = 0)
        {
            H3DMaterial Output = new H3DMaterial();

            Output.MaterialParams.EmissionColor  = RGBA.Black;
            Output.MaterialParams.AmbientColor   = RGBA.White;
            Output.MaterialParams.DiffuseColor   = RGBA.White;
            Output.MaterialParams.Specular0Color = new RGBA(128, 128, 128, 255);
            Output.MaterialParams.Specular1Color = new RGBA(128, 128, 128, 255);
            Output.MaterialParams.Constant0Color = RGBA.Black;
            Output.MaterialParams.Constant1Color = RGBA.Black;
            Output.MaterialParams.Constant2Color = RGBA.Black;
            Output.MaterialParams.Constant3Color = RGBA.Black;
            Output.MaterialParams.Constant4Color = RGBA.Black;
            Output.MaterialParams.Constant5Color = RGBA.Black;
            Output.MaterialParams.BlendColor     = RGBA.Black;

            Output.MaterialParams.ColorScale = 1;

            Output.MaterialParams.TexEnvBufferColor = RGBA.White;

            Output.MaterialParams.ColorOperation.BlendMode   = PICABlendMode.Blend;
            Output.MaterialParams.BlendFunction.ColorSrcFunc = PICABlendFunc.One;
            Output.MaterialParams.BlendFunction.ColorDstFunc = PICABlendFunc.Zero;
            Output.MaterialParams.BlendFunction.AlphaSrcFunc = PICABlendFunc.One;
            Output.MaterialParams.BlendFunction.AlphaDstFunc = PICABlendFunc.Zero;

            Output.MaterialParams.DepthColorMask.Enabled = true;

            Output.MaterialParams.DepthColorMask.DepthFunc = PICATestFunc.Lequal;

            Output.MaterialParams.DepthColorMask.RedWrite   = true;
            Output.MaterialParams.DepthColorMask.GreenWrite = true;
            Output.MaterialParams.DepthColorMask.BlueWrite  = true;
            Output.MaterialParams.DepthColorMask.AlphaWrite = true;
            Output.MaterialParams.DepthColorMask.DepthWrite = true;

            Output.MaterialParams.ColorBufferRead  = false;
            Output.MaterialParams.ColorBufferWrite = true;

            Output.MaterialParams.StencilBufferRead  = true;
            Output.MaterialParams.StencilBufferWrite = true;

            Output.MaterialParams.DepthBufferRead  = true;
            Output.MaterialParams.DepthBufferWrite = true;

            if (!string.IsNullOrEmpty(TextureName))
            {
                Output.MaterialParams.TexEnvStages[0] = PICATexEnvStage.Texture0;
                Output.MaterialParams.TexEnvStages[0].Combiner.Color = PICATextureCombinerMode.Modulate;
                Output.MaterialParams.TexEnvStages[0].Source.Color[0] = PICATextureCombinerSource.PrimaryColor;
                Output.MaterialParams.TexEnvStages[0].Source.Color[1] = PICATextureCombinerSource.Texture0;
            }
            else
                Output.MaterialParams.TexEnvStages[0] = PICATexEnvStage.VertexColor;


            Output.MaterialParams.TexEnvStages[1] = PICATexEnvStage.PassThrough;
            Output.MaterialParams.TexEnvStages[2] = PICATexEnvStage.PassThrough;
            Output.MaterialParams.TexEnvStages[3] = PICATexEnvStage.PassThrough;
            Output.MaterialParams.TexEnvStages[4] = PICATexEnvStage.PassThrough;
            Output.MaterialParams.TexEnvStages[5] = PICATexEnvStage.PassThrough;

            Output.TextureMappers[0].WrapU = PICATextureWrap.Repeat;
            Output.TextureMappers[0].WrapV = PICATextureWrap.Repeat;

            Output.TextureMappers[0].MinFilter = H3DTextureMinFilter.NearestMipmapLinear;
            Output.TextureMappers[0].MagFilter = H3DTextureMagFilter.Linear;

            Output.TextureMappers[0].BorderColor = RGBA.White;

            Output.TextureMappers[1].SamplerType = 1;
            Output.TextureMappers[2].SamplerType = 1;

            Output.MaterialParams.TextureCoords[0].Flags = H3DTextureCoordFlags.IsDirty;
            Output.MaterialParams.TextureCoords[0].ReferenceCameraIndex = -1;
            Output.MaterialParams.TextureCoords[0].Scale = Vector2.One;

            Output.MaterialParams.TextureCoords[1].Flags = H3DTextureCoordFlags.IsDirty;
            Output.MaterialParams.TextureCoords[1].ReferenceCameraIndex = -1;
            Output.MaterialParams.TextureCoords[1].Scale = Vector2.One;

            Output.MaterialParams.TextureCoords[2].Flags = H3DTextureCoordFlags.IsDirty;
            Output.MaterialParams.TextureCoords[2].ReferenceCameraIndex = -1;
            Output.MaterialParams.TextureCoords[2].Scale = Vector2.One;

            Output.MaterialParams.ShaderReference = $"{ShaderIndex}@{ShaderName}";
            Output.MaterialParams.ModelReference = $"{MaterialName}@{ModelName}";

            Output.EnabledTextures[0] = true;

            Output.Name         = MaterialName;

            if (!string.IsNullOrEmpty(TextureName))
                Output.Texture0Name = TextureName;

            return Output;
        }

        public void SetOpaque()
        {
            this.MaterialParams.RenderLayer = 0;

            this.MaterialParams.DepthColorMask.Enabled = true;
            this.MaterialParams.DepthColorMask.DepthWrite = true;
            this.MaterialParams.DepthColorMask.DepthFunc = PICATestFunc.Less;

            this.MaterialParams.AlphaTest.Enabled = false;

            this.MaterialParams.ColorOperation.FragOpMode = PICAFragOpMode.Default;
            this.MaterialParams.ColorOperation.BlendMode = PICABlendMode.Blend;

            this.MaterialParams.BlendFunction.ColorSrcFunc = PICABlendFunc.One;
            this.MaterialParams.BlendFunction.ColorDstFunc = PICABlendFunc.Zero;
            this.MaterialParams.BlendFunction.AlphaSrcFunc = PICABlendFunc.One;
            this.MaterialParams.BlendFunction.AlphaDstFunc = PICABlendFunc.Zero;
            this.MaterialParams.BlendFunction.AlphaEquation = PICABlendEquation.FuncAdd;
            this.MaterialParams.BlendFunction.ColorEquation = PICABlendEquation.FuncAdd;
            this.MaterialParams.BlendMode = GfxFragOpBlendMode.None;

            this.MaterialParams.BlendColor = new RGBA(0, 0, 0, 255);
        }

        public void SetCustom()
        {
            this.MaterialParams.RenderLayer = 1;

            this.MaterialParams.DepthColorMask.Enabled = true;
            this.MaterialParams.DepthColorMask.DepthWrite = false;
            this.MaterialParams.DepthColorMask.DepthFunc = PICATestFunc.Less;

            this.MaterialParams.AlphaTest.Enabled = false;

            this.MaterialParams.ColorOperation.FragOpMode = PICAFragOpMode.Default;

            this.MaterialParams.BlendFunction.ColorSrcFunc = PICABlendFunc.SourceAlpha;
            this.MaterialParams.BlendFunction.ColorDstFunc = PICABlendFunc.OneMinusSourceAlpha;
            this.MaterialParams.BlendFunction.AlphaSrcFunc = PICABlendFunc.SourceAlpha;
            this.MaterialParams.BlendFunction.AlphaDstFunc = PICABlendFunc.OneMinusSourceAlpha;
            this.MaterialParams.BlendFunction.AlphaEquation = PICABlendEquation.FuncAdd;
            this.MaterialParams.BlendFunction.ColorEquation = PICABlendEquation.FuncAdd;
            this.MaterialParams.BlendMode = GfxFragOpBlendMode.BlendSeparate;
        }

        public void SetTransparent()
        {
            this.MaterialParams.RenderLayer = 0;

            this.MaterialParams.DepthColorMask.Enabled = true;
            this.MaterialParams.DepthColorMask.DepthWrite = true;
            this.MaterialParams.DepthColorMask.DepthFunc = PICATestFunc.Less;

            this.MaterialParams.AlphaTest.Enabled = true;
            this.MaterialParams.AlphaTest.Reference = 128;
            this.MaterialParams.AlphaTest.Function = PICATestFunc.Greater;

            this.MaterialParams.ColorOperation.FragOpMode = PICAFragOpMode.Default;
            this.MaterialParams.ColorOperation.BlendMode = PICABlendMode.Blend;

            this.MaterialParams.BlendFunction.ColorSrcFunc = PICABlendFunc.One;
            this.MaterialParams.BlendFunction.ColorDstFunc = PICABlendFunc.Zero;
            this.MaterialParams.BlendFunction.AlphaSrcFunc = PICABlendFunc.One;
            this.MaterialParams.BlendFunction.AlphaDstFunc = PICABlendFunc.Zero;
            this.MaterialParams.BlendFunction.AlphaEquation = PICABlendEquation.FuncAdd;
            this.MaterialParams.BlendFunction.ColorEquation = PICABlendEquation.FuncAdd;

            this.MaterialParams.BlendColor = new RGBA(0, 0, 0, 255);
            this.MaterialParams.BlendMode = GfxFragOpBlendMode.None;
        }

        public void SetTranslucent()
        {
            this.MaterialParams.RenderLayer = 1;

            this.MaterialParams.DepthColorMask.Enabled = true;
            this.MaterialParams.DepthColorMask.DepthWrite = false;
            this.MaterialParams.DepthColorMask.DepthFunc = PICATestFunc.Less;

            this.MaterialParams.AlphaTest.Enabled = false;

            this.MaterialParams.ColorOperation.FragOpMode = PICAFragOpMode.Default;
            this.MaterialParams.ColorOperation.BlendMode = PICABlendMode.Blend;

            this.MaterialParams.BlendFunction.ColorSrcFunc = PICABlendFunc.SourceAlpha;
            this.MaterialParams.BlendFunction.ColorDstFunc = PICABlendFunc.OneMinusSourceAlpha;
            this.MaterialParams.BlendFunction.AlphaSrcFunc = PICABlendFunc.SourceAlpha;
            this.MaterialParams.BlendFunction.AlphaDstFunc = PICABlendFunc.OneMinusSourceAlpha;
            this.MaterialParams.BlendFunction.AlphaEquation = PICABlendEquation.FuncAdd;
            this.MaterialParams.BlendFunction.ColorEquation = PICABlendEquation.FuncAdd;
            this.MaterialParams.BlendMode = GfxFragOpBlendMode.Blend;
        }
        public H3DMaterial()
        {
            MaterialParams = new H3DMaterialParams();
            TextureMappers = new H3DTextureMapper[3];
            TextureMappersCompat = null;

            EnabledTextures = new bool[4];
        }

        void ICustomSerialization.Deserialize(BinaryDeserializer Deserializer)
        {
            PICACommandReader Reader = new PICACommandReader(TextureCommands);

            int cmd = 0;
            while (Reader.HasCommand)
            {
                PICACommand Cmd = Reader.GetCommand();

                uint Param = Cmd.Parameters[0];

                Console.WriteLine($"Register {Cmd.Register} params {string.Join(",", Cmd.Parameters)}");

                switch (Cmd.Register)
                {
                    case PICARegister.GPUREG_TEXUNIT_CONFIG:
                        EnabledTextures[0] = (Param & 0x001) != 0;
                        EnabledTextures[1] = (Param & 0x002) != 0;
                        EnabledTextures[2] = (Param & 0x004) != 0;
                        EnabledTextures[3] = (Param & 0x400) != 0;
                        break;
                }
            }

            if (TextureMappersCompat != null)
            {
                Array.Copy(TextureMappersCompat, TextureMappers, 3);
            }


            for (int i = 0; i < MaterialParams.TexEnvStages.Length; i++)
                MaterialParams.TexEnvStages[i].Constant = (GfxTexEnvConstant)MaterialParams.GetConstantIndex(i);
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            for (int i = 0; i < MaterialParams.TexEnvStages.Length; i++)
            {
                int id = (int)MaterialParams.TexEnvStages[i].Constant;
                if (id < 6)
                    MaterialParams.SetConstantIndex(i, id);
            }

            //The original tool seems to add those (usually unused) names with the silhouette suffix
            Serializer.Sections[(uint)H3DSectionId.Strings].Values.Add(new RefValue()
            {
                Position = -1,
                Value    = $"{Name}-silhouette"
            });

            PICACommandWriter Writer = new PICACommandWriter();

            uint TexUnitConfig = 0x00011000u;

            TexUnitConfig |= (EnabledTextures[0] ? 1u : 0u) << 0;
            TexUnitConfig |= (EnabledTextures[1] ? 1u : 0u) << 1;
            TexUnitConfig |= (EnabledTextures[2] ? 1u : 0u) << 2;
            TexUnitConfig |= (EnabledTextures[3] ? 1u : 0u) << 10;


            if (Serializer.CurrentRevision >= 34)
            {
                Writer.SetCommands(PICARegister.GPUREG_TEXUNIT_CONFIG, false, 0, 0, 0, 0);
                Writer.SetCommand(PICARegister.GPUREG_TEXUNIT_CONFIG, TexUnitConfig);
                Writer.SetCommand(PICARegister.GPUREG_TEXUNIT_CONFIG, 4103);

                Console.WriteLine($"TexUnitConfig {TexUnitConfig}");
            }
            else
            {
                Writer.SetCommands(PICARegister.GPUREG_TEXUNIT_CONFIG, false, 0, 0, 0, 0);
                Writer.SetCommand(PICARegister.GPUREG_TEXUNIT_CONFIG, TexUnitConfig);
            }
            /*    Writer.SetCommands(PICARegister.GPUREG_TEXUNIT_CONFIG, false, 0, 0, 0, 0, 0x1001, 0xF0080);
                else
                    Writer.SetCommands(PICARegister.GPUREG_TEXUNIT_CONFIG, false, 0, 0, 0, 0);
                */


            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT0_BORDER_COLOR, 0);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT0_DIM, 0);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT0_PARAM, 0x2220);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT0_LOD, 0);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT0_ADDR1, 0);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT0_TYPE, 0xc);

            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT1_BORDER_COLOR, 0);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT1_DIM, 0);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT1_PARAM, 0x2220);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT1_LOD, 0);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT1_ADDR, 0);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT1_TYPE, 0xc);

            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT2_BORDER_COLOR, 0);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT2_DIM, 0);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT2_PARAM, 0x2220);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT2_LOD, 0);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT2_ADDR, 0);
            Writer.SetCommand(PICARegister.GPUREG_TEXUNIT2_TYPE, 0xc);

            Writer.WriteEnd();

            TextureCommands = Writer.GetBuffer();

            if (Serializer.CurrentRevision < 0x21 && TextureMappersCompat == null)
            {
                TextureMappersCompat = new H3DTextureMapper[3];
            }

            if (TextureMappersCompat != null)
            {
                Array.Copy(TextureMappers, TextureMappersCompat, 3);
            }

            return false;
        }
    }
}
