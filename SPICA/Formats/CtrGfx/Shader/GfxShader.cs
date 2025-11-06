using SPICA.Formats.Common;
using SPICA.PICA.Shader;
using SPICA.Serialization.Attributes;
using System;

namespace SPICA.Formats.CtrGfx.Shader
{
    [TypeChoice(0x80000002u, typeof(GfxShader))]
    public class GfxShader : GfxObject
    {
        public override GfxObjRevisionsV5 Revision => GfxObjRevisionsV5.Shader;

        public byte[] ShaderData;

        public uint[] CommandsA;

        public GfxShaderInfo[] ShaderInfos;

        public uint[] CommandsB;

        [Inline, FixedLength(16)] public byte[] Padding;

        public GfxShader()
        {
            this.Header.MagicNumber = 0x52444853;
        }

        public ShaderBinary ToBinary()
        {
            return new ShaderBinary(ShaderData);
        }
    }

    public class GfxShaderInfo
    {
        public uint Flag;

        public int VertexProgramIndex;
        public int GeometryProgramIndex;

        [Inline, FixedLength(124)] public byte[] Data;
    }
}
