using SPICA.Formats.CtrGfx.Animation;
using SPICA.Formats.CtrGfx.Camera;
using SPICA.Formats.CtrGfx.Emitter;
using SPICA.Formats.CtrGfx.Fog;
using SPICA.Formats.CtrGfx.Light;
using SPICA.Formats.CtrGfx.LUT;
using SPICA.Formats.CtrGfx.Model;
using SPICA.Formats.CtrGfx.Model.Material;
using SPICA.Formats.CtrGfx.Model.Mesh;
using SPICA.Formats.CtrGfx.Scene;
using SPICA.Formats.CtrGfx.Shader;
using SPICA.Formats.CtrGfx.Texture;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Animation;
using SPICA.Formats.CtrH3D.LUT;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;
using SPICA.Serialization.Serializer;

using System;
using System.IO;
using System.Numerics;

namespace SPICA.Formats.CtrGfx
{
    enum GfxSectionId
    {
        Contents,
        Strings,
        Image
    }

    public class Gfx
    {
        [Ignore]
        public uint Revision = GfxConstants.CGFXRevision;

        public readonly GfxDict<GfxModel>     Models;
        public readonly GfxDict<GfxTexture>   Textures;
        public readonly GfxDict<GfxLUT>       LUTs;
        public readonly GfxDict<GfxMaterial>  Materials;
        public readonly GfxDict<GfxShader>    Shaders;
        public readonly GfxDict<GfxCamera>    Cameras;
        public readonly GfxDict<GfxLight>     Lights;
        public readonly GfxDict<GfxFog>       Fogs;
        public readonly GfxDict<GfxScene>     Scenes;
        public readonly GfxDict<GfxAnimation> SkeletalAnimations;
        public readonly GfxDict<GfxAnimation> MaterialAnimations;
        public readonly GfxDict<GfxAnimation> VisibilityAnimations;
        public readonly GfxDict<GfxAnimation> CameraAnimations;
        public readonly GfxDict<GfxAnimation> LightAnimations;
        public readonly GfxDict<GfxAnimation> FogAnimations;
        public readonly GfxDict<GfxEmitter>   Emitters;

        public Gfx()
        {
            Models               = new GfxDict<GfxModel>();
            Textures             = new GfxDict<GfxTexture>();
            LUTs                 = new GfxDict<GfxLUT>();
            Materials            = new GfxDict<GfxMaterial>();
            Shaders              = new GfxDict<GfxShader>();
            Cameras              = new GfxDict<GfxCamera>();
            Lights               = new GfxDict<GfxLight>();
            Fogs                 = new GfxDict<GfxFog>();
            Scenes               = new GfxDict<GfxScene>();
            SkeletalAnimations   = new GfxDict<GfxAnimation>();
            MaterialAnimations   = new GfxDict<GfxAnimation>();
            VisibilityAnimations = new GfxDict<GfxAnimation>();
            CameraAnimations     = new GfxDict<GfxAnimation>();
            LightAnimations      = new GfxDict<GfxAnimation>();
            FogAnimations        = new GfxDict<GfxAnimation>();
            Emitters             = new GfxDict<GfxEmitter>();
        }

        public static Gfx Open(string FileName)
        {
            using (FileStream Input = new FileStream(FileName, FileMode.Open))
            {
                BinaryDeserializer Deserializer = new BinaryDeserializer(Input, GetSerializationOptions());

                GfxHeader Header = Deserializer.Deserialize<GfxHeader>();
                Gfx       Scene  = Deserializer.Deserialize<Gfx>();

                Scene.Revision   = Header.Revision;

                return Scene;
            }
        }

        public static Gfx Open(Stream Input)
        {
            BinaryDeserializer Deserializer = new BinaryDeserializer(Input, GetSerializationOptions());

            GfxHeader Header = Deserializer.Deserialize<GfxHeader>();
            Gfx Scene = Deserializer.Deserialize<Gfx>();

            return Scene;
        }

        public static H3D OpenAsH3D(Stream Input)
        {
            BinaryDeserializer Deserializer = new BinaryDeserializer(Input, GetSerializationOptions());

            GfxHeader Header = Deserializer.Deserialize<GfxHeader>();
            Gfx       Scene  = Deserializer.Deserialize<Gfx>();

            return Scene.ToH3D();
        }

        public H3D ToH3D()
        {
            H3D Output = new H3D();

            foreach (GfxModel Model in Models)
                Output.Models.Add(Model.ToH3D());

            foreach (GfxTexture Texture in Textures)
            {
                H3DTexture Tex = new H3DTexture()
                {
                    Name       = Texture.Name,
                    Width      = Texture.Width,
                    Height     = Texture.Height,
                    Format     = Texture.HwFormat,
                    MipmapSize = (byte)Texture.MipmapSize
                };

                if (Texture is GfxTextureCube)
                {
                    Tex.RawBufferXPos = ((GfxTextureCube)Texture).ImageXPos.RawBuffer;
                    Tex.RawBufferXNeg = ((GfxTextureCube)Texture).ImageXNeg.RawBuffer;
                    Tex.RawBufferYPos = ((GfxTextureCube)Texture).ImageYPos.RawBuffer;
                    Tex.RawBufferYNeg = ((GfxTextureCube)Texture).ImageYNeg.RawBuffer;
                    Tex.RawBufferZPos = ((GfxTextureCube)Texture).ImageZPos.RawBuffer;
                    Tex.RawBufferZNeg = ((GfxTextureCube)Texture).ImageZNeg.RawBuffer;
                }
                else
                {
                    Tex.RawBuffer = ((GfxTextureImage)Texture).Image.RawBuffer;
                }

                Output.Textures.Add(Tex);
            }

            foreach (GfxLUT LUT in LUTs)
            {
                H3DLUT L = new H3DLUT() { Name = LUT.Name };

                foreach (GfxLUTSampler Sampler in LUT.Samplers)
                {
                    L.Samplers.Add(new H3DLUTSampler()
                    {
                        Flags = Sampler.IsAbsolute ? H3DLUTFlags.IsAbsolute : 0,
                        Name  = Sampler.Name,
                        Table = Sampler.Table
                    });
                }

                Output.LUTs.Add(L);
            }

            foreach (GfxScene Scene in Scenes)
            {
                Output.Scenes.Add(Scene.ToH3D());
            }

            foreach (GfxFog Fog in Fogs)
            {
                Output.Fogs.Add(Fog.ToH3D());
            }

            foreach (GfxCamera Camera in Cameras)
            {
                Output.Cameras.Add(Camera.ToH3DCamera());
            }

            foreach (GfxLight Light in Lights)
            {
                Output.Lights.Add(Light.ToH3DLight());
            }

            foreach (GfxAnimation SklAnim in SkeletalAnimations)
            {
                Output.SkeletalAnimations.Add(SklAnim.ToH3DAnimation());
            }

            foreach (GfxAnimation MatAnim in MaterialAnimations)
            {
                Output.MaterialAnimations.Add(new H3DMaterialAnim(MatAnim.ToH3DAnimation()));
            }

            foreach (GfxAnimation VisAnim in VisibilityAnimations)
            {
                Output.VisibilityAnimations.Add(VisAnim.ToH3DAnimation());
            }

            foreach (GfxAnimation CamAnim in CameraAnimations)
            {
                Output.CameraAnimations.Add((H3DCameraAnim)CamAnim.ToH3DAnimation());
            }

            Output.CopyMaterials();

            return Output;
        }

        public static void Save(string FileName, Gfx Scene)
        {
            using (FileStream FS = new FileStream(FileName, FileMode.Create)) {
                Save(FS, Scene);
            }
        }

        public static void Save(Stream FS, Gfx Scene)
        {
            GfxHeader Header = new GfxHeader();
            Header.Revision = Scene.Revision;

            BinarySerializer Serializer = new BinarySerializer(FS, GetSerializationOptions(), true);

            Section Contents = Serializer.Sections[(uint)H3DSectionId.Contents];

            Contents.Header = Header;

            Section Strings = new Section();
            Section Image = new Section();
            Image.Header = new GfxSectionHeader("IMAG");

            Serializer.AddSection((uint)GfxSectionId.Strings, Strings, typeof(string));
            Serializer.AddSection((uint)GfxSectionId.Strings, Strings, typeof(GfxStringUtf8));
            Serializer.AddSection((uint)GfxSectionId.Strings, Strings, typeof(GfxStringUtf16LE));
            Serializer.AddSection((uint)GfxSectionId.Strings, Strings, typeof(GfxStringUtf16BE));
            Serializer.AddSection((uint)GfxSectionId.Image, Image);

            Serializer.Serialize(Scene);

            Header.FileLength = (int)FS.Length;

            Header.SectionsCount = Image.Values.Count > 0 ? 2 : 1;

            Header.Data.Length = Contents.Length + Strings.Length + 8;

            FS.Seek(0, SeekOrigin.Begin);

            Serializer.WriteValue(Header);

            FS.Seek(Image.Position - 4, SeekOrigin.Begin);

            Serializer.Writer.Write(Image.LengthWithHeader);
        }

        private static SerializationOptions GetSerializationOptions()
        {
            return new SerializationOptions(LengthPos.BeforePtr, PointerType.SelfRelative);
        }
    }
}
