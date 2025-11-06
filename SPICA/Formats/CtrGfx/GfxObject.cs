using SPICA.Formats.Common;
using SPICA.Formats.CtrGfx.Camera;
using SPICA.Formats.CtrGfx.Light;
using SPICA.Formats.CtrGfx.LUT;
using SPICA.Formats.CtrGfx.Model;
using SPICA.Formats.CtrGfx.Model.Material;
using SPICA.Formats.CtrGfx.Model.Mesh;
using SPICA.Formats.CtrGfx.Texture;
using SPICA.Formats.CtrGfx.Shader;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx
{
    public enum GfxObjRevisionsV5 : uint
    {
        Skeleton = 0x00000000,
        Model = 0x09000000,
        Mesh = 0x00000000,
        Shape = 0x00000000,
        Material = 0x06000003,
        TextureRef = 0x05000000,
        TextureImage = 0x05000000,
        ShaderRef = 0x06000000,
        LUT = 0x04000100,
        Shader = 0x06000000,
        Light = 0x07010000,

        //TODO:
        Scene = 0,
    }

    internal enum GfxObjTypesV5 : uint
    {
        None = 0xFFFFFFFF,

        Skeleton = 0x02000000,
        SkeletalModel = 0x40000092,
        Model = 0x40000012,
        Material = 0x08000000,
        Mesh = 0x01000000,
        Shape = 0x10000001,
        TextureImage = 0x20000011,
        TextureMapper = 0x80000000,
        TextureRef = 0x20000004,
        TextureSampler = 0x80000000u,
        ShaderRef = 0x80000001,
        LUT = 0x04000000,
        LUTSampler = 0x80000000,
    }

    [TypeChoice(0x01000000, typeof(GfxMesh), GfxObjTypesV5.Mesh)]
    [TypeChoice(0x02000000, typeof(GfxSkeleton), GfxObjTypesV5.SkeletalModel)]
    [TypeChoice(0x04000000, typeof(GfxLUT), GfxObjTypesV5.LUT)]
    [TypeChoice(0x08000000, typeof(GfxMaterial), GfxObjTypesV5.Material)]
    [TypeChoice(0x10000001, typeof(GfxShape), GfxObjTypesV5.Shape)]
    [TypeChoice(0x20000004, typeof(GfxTextureReference), GfxObjTypesV5.TextureRef)]
    [TypeChoice(0x20000009, typeof(GfxTextureCube))]
    [TypeChoice(0x20000011, typeof(GfxTextureImage), GfxObjTypesV5.TextureImage)]
    [TypeChoice(0x4000000a, typeof(GfxCamera))]
    [TypeChoice(0x00041202, typeof(GfxModel), GfxObjTypesV5.Model)]
    [TypeChoice(0x40000012, typeof(GfxModel), GfxObjTypesV5.Model)]
    [TypeChoice(0x00041202, typeof(GfxModel), GfxObjTypesV5.Model)]
    [TypeChoice(0x40000092, typeof(GfxModelSkeletal), GfxObjTypesV5.SkeletalModel)]
    [TypeChoice(0x400000a2, typeof(GfxFragmentLight))]
    [TypeChoice(0x40000122, typeof(GfxHemisphereLight))]
    [TypeChoice(0x40000222, typeof(GfxVertexLight))]
    [TypeChoice(0x40000422, typeof(GfxAmbientLight))]
    [TypeChoice(0x80000001, typeof(GfxShaderReference), GfxObjTypesV5.ShaderRef)]
    [TypeChoice(0x80000002, typeof(GfxShader))]
    public abstract class GfxObject : INamed
    {
        public abstract GfxObjRevisionsV5 Revision { get; }

        internal GfxRevHeader Header;

        private string _Name;

        public string Name
        {
            get => _Name;
            set => _Name = value ?? throw Exceptions.GetNullException("Name");
        }

        public GfxDict<GfxMetaData> MetaData;

        public GfxObject()
        {
            MetaData = new GfxDict<GfxMetaData>();
        }
    }
}
