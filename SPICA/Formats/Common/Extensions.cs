using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPICA.Formats.CtrGfx.Model.Material;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.Formats.GFL2.Model.Material;
using SPICA.PICA.Commands;

namespace SPICA.Formats.Common
{
    internal enum GLTexFormat : uint
    {
        RGBA8 = 0x14016752,
        RGB8 = 0x14016754,
        RGBA5551 = 0x80346752,
        RGB565 = 0x83636754,
        RGBA4 = 0x80336752,
        A8 = 0x14016756,
        L8 = 0x14016757,
        LA8 = 0x14016758,
        RG8 = 0x14016759,
        A4 = 0x67616756,
        L4 = 0x67616757,
        LA4 = 0x67606758,
        ETC1 = 0x0000675A,
        ETC1a4 = 0x0000675B,
    }

    public enum GLTexCombineOp : uint
    {
        Color = 0x0300,
        OneMinusColor = 0x0301,
        Alpha = 0x0302,
        OneMinusAlpha = 0x0303,
        Red = 0x8580,
        OneMinusRed = 0x8583,
        Green = 0x8581,
        OneMinusGreen = 0x8584,
        Blue = 0x8582,
        OneMinusBlue = 0x8585
    }

    public enum GLTexCombinerMode : uint
    {
        Replace = 0x1E01,
        Modulate = 0x2100,
        Add = 0x0104,
        AddSigned = 0x8574,
        Interpolate = 0x8575,
        Subtract = 0x84E7,
        DotProduct3Rgb = 0x86AE,
        DotProduct3Rgba = 0x86AF,
        MultAdd = 0x6401,
        AddMult = 0x6402
    }

    public enum GLTexCombinerSrc : uint
    {
        PrimaryColor = 0x8577,
        FragmentPrimaryColor = 0x6210,
        FragmentSecondaryColor = 0x6211,
        Texture0 = 0x84C0,
        Texture1 = 0x84C1,
        Texture2 = 0x84C2,
        Texture3 = 0x84C3,
        PreviousBuffer = 0x8579,
        ConstantCol = 0x8576,
        Previous = 0x8578
    }

    public enum GLLayerConfig : uint
    {
        LayerConfig0 = 25264,
        LayerConfig1 = 25265,
        LayerConfig2 = 25266,
        LayerConfig3 = 25267,
        LayerConfig4 = 25268,
        LayerConfig5 = 25269,
        LayerConfig6 = 25270,
        LayerConfig7 = 25271
    }

    public enum GLLUTInput : uint
    {
        CosNormalHalf = 25248,
        CosViewHalf = 25249,
        CosNormalView = 25250,
        CosLightNormal = 25251,
        CosLightSpot = 25252,
        CosPhi = 25253
    }

    public enum GLFresnelSelector : uint
    {
        No = 25280,
        Pri = 25281,
        Sec = 25282,
        PriSec = 25283
    }

    public enum GLBumpMode : uint
    {
        NotUsed = 25288,
        AsBump = 25289,
        AsTangent = 25290
    }

    public enum GLBumpTexture : uint
    {
        Texture0 = 33984,
        Texture1 = 33985,
        Texture2 = 33986,
        Texture3 = 33987
    }

    public enum GLTexMinFilter : uint
    {
        Nearest = 0x2600,
        Linear = 0x2601,
        Nearest_Mipmap_Nearest = 0x2700,
        Linear_Mipmap_Nearest = 0x2701,
        Nearest_Mipmap_Linear = 0x2702,
        Linear_Mipmap_Linear = 0x2703,
    }

    public enum GLTexMagFilter : uint
    {
        Nearest = 0x2600,
        Linear = 0x2601
    }

    public enum GLTexWrap : uint
    {
        ClampToBorder = 0x812D,
        Repeat = 0x2901,
        ClampToEdge = 0x812F,
        Mirror = 0x8370
    }

    internal enum GLBlendFunc : uint
    {
        Zero = 0,
        One = 1,
        SourceColor = 768,
        OneMinusSourceColor = 769,
        DestinationColor = 774,
        OneMinusDestinationColor = 775,
        SourceAlpha = 770,
        OneMinusSourceAlpha = 771,
        DestinationAlpha = 772,
        OneMinusDestinationAlpha = 773,
        ConstantColor = 32769,
        OneMinusConstantColor = 32770,
        ConstantAlpha = 32771,
        OneMinusConstantAlpha = 32772,
        SourceAlphaSaturate = 776
    }

    internal enum GLBlendEquation : uint
    {
        FuncAdd = 32774,
        FuncSubtract = 32778,
        FuncReverseSubtract = 32779,
        Min = 32775,
        Max = 32776
    }

    internal enum GLTestFunc : uint
    {
        Never = 0x200,
        Less = 0x201,
        Equal = 0x202,
        Lequal = 0x203,
        Greater = 0x204,
        NotEqual = 0x205,
        Gequal = 0x206,
        Always = 0x207
    }

    internal enum GLStencilOp : uint
    {
        Zero = 0,
        Keep = 0x1E00,
        Replace = 0x1E01,
        Increment = 0x1E02,
        Decrement = 0x1E03,
        Invert = 0x150A,
        IncrementWrap = 0x8507,
        DecrementWrap = 0x8508
    }

    internal enum GLLogicOp : uint
    {
        Clear = 0x1500,
        And = 0x1501,
        AndReverse = 0x1502,
        Copy = 0x1503,
        AndInverted = 0x1504,
        Noop = 0x1505,
        Xor = 0x1506,
        Or = 0x1507,
        Nor = 0x1508,
        Equiv = 0x1509,
        Invert = 0x150A,
        OrReverse = 0x150B,
        CopyInverted = 0x150C,
        OrInverted = 0x150D,
        Nand = 0x150E,
        Set = 0x150F
    }

    internal enum GLFaceCulling : uint
    {
        FrontFace = 1028u,
        BackFace = 1029u,
        Always = 1032u,
        Never = 0
    }

    internal static class Extensions
    {
        public static GfxFaceCulling ToGFX(this GLFaceCulling FaceCulling)
        {
            return FaceCulling switch
            {
                GLFaceCulling.FrontFace => GfxFaceCulling.FrontFace,
                GLFaceCulling.BackFace => GfxFaceCulling.BackFace,
                GLFaceCulling.Always => GfxFaceCulling.Always,
                GLFaceCulling.Never => GfxFaceCulling.Never,
                _ => throw new ArgumentException($"Invalid Face culling {FaceCulling}!"),
            };
        }

        public static PICATextureFormat ToPICA(this GLTexFormat format)
        {

            if (((uint)format & 0xFFFF) == 0x675A) return PICATextureFormat.ETC1;
            else if (((uint)format & 0xFFFF) == 0x675B) return PICATextureFormat.ETC1A4;

            return format switch
            {
                GLTexFormat.RGBA8 => PICATextureFormat.RGBA8,
                GLTexFormat.RGB8 => PICATextureFormat.RGB8,
                GLTexFormat.RGBA5551 => PICATextureFormat.RGBA5551,
                GLTexFormat.RGB565 => PICATextureFormat.RGB565,
                GLTexFormat.RGBA4 => PICATextureFormat.RGBA4,
                GLTexFormat.LA8 => PICATextureFormat.LA8,
                GLTexFormat.RG8 => PICATextureFormat.HiLo8,
                GLTexFormat.L8 => PICATextureFormat.L8,
                GLTexFormat.A8 => PICATextureFormat.A8,
                GLTexFormat.LA4 => PICATextureFormat.LA4,
                GLTexFormat.L4 => PICATextureFormat.L4,
                GLTexFormat.A4 => PICATextureFormat.A4,
                _ => throw new Exception($"Unsupported texture format {format}"),
            };
        }

        public static GfxLayerConfig ToGFX(this GLLayerConfig config)
        {
            return config switch
            {
                GLLayerConfig.LayerConfig0 => GfxLayerConfig.LayerConfig0,
                GLLayerConfig.LayerConfig1 => GfxLayerConfig.LayerConfig1,
                GLLayerConfig.LayerConfig2 => GfxLayerConfig.LayerConfig2,
                GLLayerConfig.LayerConfig3 => GfxLayerConfig.LayerConfig3,
                GLLayerConfig.LayerConfig4 => GfxLayerConfig.LayerConfig4,
                GLLayerConfig.LayerConfig5 => GfxLayerConfig.LayerConfig5,
                GLLayerConfig.LayerConfig6 => GfxLayerConfig.LayerConfig6,
                GLLayerConfig.LayerConfig7 => GfxLayerConfig.LayerConfig7,
                _ => throw new ArgumentException($"Invalid Layer Config {config}!"),
            };
        }

        public static PICATextureCombinerAlphaOp ToPICA_Alpha(this GLTexCombineOp CombinerOp)
        {
            return CombinerOp switch
            {
                GLTexCombineOp.Alpha => PICATextureCombinerAlphaOp.Alpha,
                GLTexCombineOp.OneMinusAlpha => PICATextureCombinerAlphaOp.OneMinusAlpha,
                GLTexCombineOp.Red => PICATextureCombinerAlphaOp.Red,
                GLTexCombineOp.OneMinusRed => PICATextureCombinerAlphaOp.OneMinusRed,
                GLTexCombineOp.Green => PICATextureCombinerAlphaOp.Green,
                GLTexCombineOp.OneMinusGreen => PICATextureCombinerAlphaOp.OneMinusGreen,
                GLTexCombineOp.Blue => PICATextureCombinerAlphaOp.Blue,
                GLTexCombineOp.OneMinusBlue => PICATextureCombinerAlphaOp.OneMinusBlue,
                _ => throw new ArgumentException($"Invalid Combiner Alpha Op {CombinerOp}!"),
            };
        }

        public static PICATextureCombinerColorOp ToPICA_Color(this GLTexCombineOp CombinerOp)
        {
            return CombinerOp switch
            {
                GLTexCombineOp.Color => PICATextureCombinerColorOp.Color,
                GLTexCombineOp.OneMinusColor => PICATextureCombinerColorOp.OneMinusColor,
                GLTexCombineOp.Alpha => PICATextureCombinerColorOp.Alpha,
                GLTexCombineOp.OneMinusAlpha => PICATextureCombinerColorOp.OneMinusAlpha,
                GLTexCombineOp.Red => PICATextureCombinerColorOp.Red,
                GLTexCombineOp.OneMinusRed => PICATextureCombinerColorOp.OneMinusRed,
                GLTexCombineOp.Green => PICATextureCombinerColorOp.Green,
                GLTexCombineOp.OneMinusGreen => PICATextureCombinerColorOp.OneMinusGreen,
                GLTexCombineOp.Blue => PICATextureCombinerColorOp.Blue,
                GLTexCombineOp.OneMinusBlue => PICATextureCombinerColorOp.OneMinusBlue,
                _ => throw new ArgumentException($"Invalid Combiner Color Op {CombinerOp}!"),
            };
        }

        public static PICATextureCombinerMode ToPICA(this GLTexCombinerMode CombinerMode)
        {
            return CombinerMode switch
            {
                GLTexCombinerMode.Replace => PICATextureCombinerMode.Replace,
                GLTexCombinerMode.Modulate => PICATextureCombinerMode.Modulate,
                GLTexCombinerMode.Add => PICATextureCombinerMode.Add,
                GLTexCombinerMode.AddSigned => PICATextureCombinerMode.AddSigned,
                GLTexCombinerMode.Interpolate => PICATextureCombinerMode.Interpolate,
                GLTexCombinerMode.Subtract => PICATextureCombinerMode.Subtract,
                GLTexCombinerMode.DotProduct3Rgb => PICATextureCombinerMode.DotProduct3Rgb,
                GLTexCombinerMode.DotProduct3Rgba => PICATextureCombinerMode.DotProduct3Rgba,
                GLTexCombinerMode.MultAdd => PICATextureCombinerMode.MultAdd,
                GLTexCombinerMode.AddMult => PICATextureCombinerMode.AddMult,
                _ => throw new ArgumentException($"Invalid Combiner Mode {CombinerMode}!"),
            };
        }
        public static PICATextureCombinerSource ToPICA(this GLTexCombinerSrc CombinerSrc)
        {
            return CombinerSrc switch
            {
                GLTexCombinerSrc.PrimaryColor => PICATextureCombinerSource.PrimaryColor,
                GLTexCombinerSrc.FragmentPrimaryColor => PICATextureCombinerSource.FragmentPrimaryColor,
                GLTexCombinerSrc.FragmentSecondaryColor => PICATextureCombinerSource.FragmentSecondaryColor,
                GLTexCombinerSrc.Texture0 => PICATextureCombinerSource.Texture0,
                GLTexCombinerSrc.Texture1 => PICATextureCombinerSource.Texture1,
                GLTexCombinerSrc.Texture2 => PICATextureCombinerSource.Texture2,
                GLTexCombinerSrc.Texture3 => PICATextureCombinerSource.Texture3,
                GLTexCombinerSrc.PreviousBuffer => PICATextureCombinerSource.PreviousBuffer,
                GLTexCombinerSrc.ConstantCol => PICATextureCombinerSource.Constant,
                GLTexCombinerSrc.Previous => PICATextureCombinerSource.Previous,
                _ => throw new ArgumentException($"Invalid Combiner Src {CombinerSrc}!"),
            };
        }
        public static int ToInt(this GLBumpTexture BumpTex)
        {
            return BumpTex switch
            {
                GLBumpTexture.Texture0 => 0,
                GLBumpTexture.Texture1 => 1,
                GLBumpTexture.Texture2 => 2,
                GLBumpTexture.Texture3 => 3,
                _ => throw new ArgumentException($"Invalid Bump Texture Index {BumpTex}!"),
            };
        }

        public static GfxBumpMode ToPICA(this GLBumpMode BumpMode)
        {
            return BumpMode switch
            {
                GLBumpMode.NotUsed => GfxBumpMode.NotUsed,
                GLBumpMode.AsBump => GfxBumpMode.AsBump,
                GLBumpMode.AsTangent => GfxBumpMode.AsTangent,
                _ => throw new ArgumentException($"Invalid Bump Mode {BumpMode}!"),
            };
        }

        public static GfxFresnelSelector ToPICA(this GLFresnelSelector FresnelSelector)
        {
            return FresnelSelector switch
            {
                GLFresnelSelector.No => GfxFresnelSelector.No,
                GLFresnelSelector.Pri => GfxFresnelSelector.Pri,
                GLFresnelSelector.Sec => GfxFresnelSelector.Sec,
                GLFresnelSelector.PriSec => GfxFresnelSelector.PriSec,
                _ => throw new ArgumentException($"Invalid Fresnel Selector {FresnelSelector}!")
            };
        }

        public static PICALUTInput ToPICA(this GLLUTInput LutInput)
        {
            return LutInput switch
            {
                GLLUTInput.CosNormalHalf => PICALUTInput.CosNormalHalf,
                GLLUTInput.CosViewHalf => PICALUTInput.CosViewHalf,
                GLLUTInput.CosNormalView => PICALUTInput.CosNormalView,
                GLLUTInput.CosLightNormal => PICALUTInput.CosLightNormal,
                GLLUTInput.CosLightSpot => PICALUTInput.CosLightSpot,
                GLLUTInput.CosPhi => PICALUTInput.CosPhi,
                _ => throw new ArgumentException($"Invalid Lut Input {LutInput}!")
            };
        }

        public static PICATextureWrap ToPICA(this GLTexWrap FaceCulling)
        {
            return FaceCulling switch
            {
                GLTexWrap.Repeat => PICATextureWrap.Repeat,
                GLTexWrap.Mirror => PICATextureWrap.Mirror,
                GLTexWrap.ClampToEdge => PICATextureWrap.ClampToEdge,
                GLTexWrap.ClampToBorder => PICATextureWrap.ClampToBorder,
                _ => throw new ArgumentException($"Invalid Wrap Mode {FaceCulling}!"),
            };
        }

        public static PICATextureFilter ToPICA(this GLTexMagFilter FaceCulling)
        {
            switch (FaceCulling)
            {
                case GLTexMagFilter.Nearest: return PICATextureFilter.Nearest;
                case GLTexMagFilter.Linear: return PICATextureFilter.Linear;

                default: throw new ArgumentException($"Invalid Min Filter {FaceCulling}!");
            }
        }

        public static PICATextureFilter GetPICAMipFilter(this GLTexMinFilter filter)
        {
            switch (filter)
            {
                case GLTexMinFilter.Nearest_Mipmap_Nearest:
                case GLTexMinFilter.Linear_Mipmap_Nearest:
                    return PICATextureFilter.Nearest;

                case GLTexMinFilter.Nearest:
                case GLTexMinFilter.Linear:
                case GLTexMinFilter.Nearest_Mipmap_Linear:
                case GLTexMinFilter.Linear_Mipmap_Linear:
                    return PICATextureFilter.Linear;

                default: throw new ArgumentException($"Invalid Mip Filter {filter}!");
            }
        }

        public static PICATextureFilter GetPICAMinFilter(this GLTexMinFilter filter)
        {
            switch (filter)
            {
                case GLTexMinFilter.Nearest:
                case GLTexMinFilter.Nearest_Mipmap_Nearest:
                case GLTexMinFilter.Nearest_Mipmap_Linear:
                    return PICATextureFilter.Nearest;

                case GLTexMinFilter.Linear:
                case GLTexMinFilter.Linear_Mipmap_Nearest:
                case GLTexMinFilter.Linear_Mipmap_Linear:
                    return PICATextureFilter.Linear;

                default: throw new ArgumentException($"Invalid Min Filter {filter}!");
            }
        }


        public static H3DTextureMinFilter ToH3D(this GLTexMinFilter FaceCulling)
        {
            switch (FaceCulling)
            {
                case GLTexMinFilter.Nearest: return H3DTextureMinFilter.Nearest;
                case GLTexMinFilter.Nearest_Mipmap_Nearest: return H3DTextureMinFilter.NearestMipmapNearest;
                case GLTexMinFilter.Nearest_Mipmap_Linear: return H3DTextureMinFilter.NearestMipmapLinear;
                case GLTexMinFilter.Linear: return H3DTextureMinFilter.Linear;
                case GLTexMinFilter.Linear_Mipmap_Nearest: return H3DTextureMinFilter.LinearMipmapNearest;
                case GLTexMinFilter.Linear_Mipmap_Linear: return H3DTextureMinFilter.LinearMipmapLinear;

                default: throw new ArgumentException($"Invalid Min Filter {FaceCulling}!");
            }
        }

        public static H3DTextureMagFilter ToH3D(this GLTexMagFilter FaceCulling)
        {
            return FaceCulling switch
            {
                GLTexMagFilter.Nearest => H3DTextureMagFilter.Nearest,
                GLTexMagFilter.Linear => H3DTextureMagFilter.Linear,
                _ => throw new ArgumentException($"Invalid Mag Filter {FaceCulling}!"),
            };
        }

        public static PICABlendEquation ToPICA(this GLBlendEquation BlendEq)
        {
            return BlendEq switch
            {
                GLBlendEquation.FuncAdd => PICABlendEquation.FuncAdd,
                GLBlendEquation.FuncSubtract => PICABlendEquation.FuncSubtract,
                GLBlendEquation.FuncReverseSubtract => PICABlendEquation.FuncReverseSubtract,
                GLBlendEquation.Min => PICABlendEquation.Min,
                GLBlendEquation.Max => PICABlendEquation.Max,
                _ => throw new ArgumentException($"Invalid Blend Equation {BlendEq}!"),
            };
        }

        public static PICABlendFunc ToPICA(this GLBlendFunc BlendFunc)
        {
            return BlendFunc switch
            {
                GLBlendFunc.Zero => PICABlendFunc.Zero,
                GLBlendFunc.One => PICABlendFunc.One,
                GLBlendFunc.SourceColor => PICABlendFunc.SourceColor,
                GLBlendFunc.OneMinusSourceColor => PICABlendFunc.OneMinusSourceColor,
                GLBlendFunc.DestinationColor => PICABlendFunc.DestinationColor,
                GLBlendFunc.OneMinusDestinationColor => PICABlendFunc.OneMinusDestinationColor,
                GLBlendFunc.SourceAlpha => PICABlendFunc.SourceAlpha,
                GLBlendFunc.OneMinusSourceAlpha => PICABlendFunc.OneMinusSourceAlpha,
                GLBlendFunc.DestinationAlpha => PICABlendFunc.DestinationAlpha,
                GLBlendFunc.OneMinusDestinationAlpha => PICABlendFunc.OneMinusDestinationAlpha,
                GLBlendFunc.ConstantColor => PICABlendFunc.ConstantColor,
                GLBlendFunc.OneMinusConstantColor => PICABlendFunc.OneMinusConstantColor,
                GLBlendFunc.ConstantAlpha => PICABlendFunc.ConstantAlpha,
                GLBlendFunc.OneMinusConstantAlpha => PICABlendFunc.OneMinusConstantAlpha,
                GLBlendFunc.SourceAlphaSaturate => PICABlendFunc.SourceAlphaSaturate,
                _ => throw new ArgumentException($"Invalid Blend Function {BlendFunc}!"),
            };
        }

        public static PICALogicalOp ToPICA(this GLLogicOp LogicOp)
        {
            return LogicOp switch
            {
                GLLogicOp.Clear => PICALogicalOp.Clear,
                GLLogicOp.And => PICALogicalOp.And,
                GLLogicOp.AndReverse => PICALogicalOp.AndReverse,
                GLLogicOp.Copy => PICALogicalOp.Copy,
                GLLogicOp.Set => PICALogicalOp.Set,
                GLLogicOp.CopyInverted => PICALogicalOp.CopyInverted,
                GLLogicOp.Noop => PICALogicalOp.Noop,
                GLLogicOp.Invert => PICALogicalOp.Invert,
                GLLogicOp.Nand => PICALogicalOp.Nand,
                GLLogicOp.Or => PICALogicalOp.Or,
                GLLogicOp.Nor => PICALogicalOp.Nor,
                GLLogicOp.Xor => PICALogicalOp.Xor,
                GLLogicOp.Equiv => PICALogicalOp.Equiv,
                GLLogicOp.AndInverted => PICALogicalOp.AndInverted,
                GLLogicOp.OrReverse => PICALogicalOp.OrReverse,
                GLLogicOp.OrInverted => PICALogicalOp.OrInverted,
                _ => throw new ArgumentException($"Invalid Logical Op {LogicOp}!"),
            };
        }

        public static PICAStencilOp ToPICA(this GLStencilOp StencilOp)
        {
            return StencilOp switch
            {
                GLStencilOp.Keep => PICAStencilOp.Keep,
                GLStencilOp.Zero => PICAStencilOp.Zero,
                GLStencilOp.Replace => PICAStencilOp.Replace,
                GLStencilOp.Increment => PICAStencilOp.Increment,
                GLStencilOp.Decrement => PICAStencilOp.Decrement,
                GLStencilOp.IncrementWrap => PICAStencilOp.IncrementWrap,
                GLStencilOp.DecrementWrap => PICAStencilOp.DecrementWrap,
                GLStencilOp.Invert => PICAStencilOp.Invert,
                _ => throw new ArgumentException($"Invalid Stencil Op {StencilOp}!"),
            };
        }

        public static PICATestFunc ToPICA(this GLTestFunc TestFunc)
        {
            return TestFunc switch
            {
                GLTestFunc.Never => PICATestFunc.Never,
                GLTestFunc.Less => PICATestFunc.Less,
                GLTestFunc.Equal => PICATestFunc.Equal,
                GLTestFunc.Lequal => PICATestFunc.Lequal,
                GLTestFunc.Greater => PICATestFunc.Greater,
                GLTestFunc.NotEqual => PICATestFunc.Notequal,
                GLTestFunc.Gequal => PICATestFunc.Gequal,
                GLTestFunc.Always => PICATestFunc.Always,
                _ => throw new ArgumentException($"Invalid Test function {TestFunc}!"),
            };
        }
    }
}
