using SPICA.Formats.Common;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Material
{
    [TypeChoice(0x80000001u, typeof(GfxShaderReference))]
    [TypeChoice(0x00000101u, typeof(GfxShaderReference))]
    public class GfxShaderReference : GfxObject
    {
        public override GfxObjRevisionsV5 Revision => GfxObjRevisionsV5.ShaderRef;

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
            this.Path = "DefaultShader";
        }
    }
}
