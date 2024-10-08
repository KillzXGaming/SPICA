﻿using SPICA.Formats.Common;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Material
{
    [TypeChoice(0x80000001u, typeof(GfxShaderReference))]
    public class GfxShaderReference : GfxObject
    {
        private string _Path;

        public string Path
        {
            get => _Path;
            set => _Path = value ?? throw Exceptions.GetNullException("Path");
        }

        private uint ShaderPtr;

        public GfxShaderReference()
        {
            this.Header.MagicNumber = 0x52444853;
            this.Header.Revision = 0x6000000;
            this.Path = "DefaultShader";
        }
    }
}
