using SPICA.PICA.Commands;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Mesh
{
    [TypeChoice(0x40000001u, typeof(GfxAttribute))]
    [TypeChoice(0x40000002u, typeof(GfxVertexBufferInterleaved))]
    [TypeChoice(0x80000000u, typeof(GfxVertexBufferFixed))]
    [TypeChoice(2u, typeof(GfxAttributeV1))] // v4.0 <
    [TypeChoice(1u, typeof(GfxVertexBufferFixed))]
    public class GfxVertexBuffer
    {
        public PICAAttributeName AttrName;

        public GfxVertexBufferType Type;
    }
}
