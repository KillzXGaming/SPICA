using System.IO;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SPICA.Math3D;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Material
{
    public struct GfxTextureCoord : ICustomSerialization
    {
        public int SourceCoordIndex;

        [JsonConverter(typeof(StringEnumConverter))]
        public GfxTextureMappingType MappingType;

        public int ReferenceCameraIndex;

        [JsonConverter(typeof(StringEnumConverter))]
        public GfxTextureTransformType TransformType;

        public Vector2 Scale;
        public float   Rotation;
        public Vector2 Translation;

        [IfVersion(CmpOp.Greater, 0x02000000)] private uint Flags; //Enabled/Dirty, set by game, SBZ

        [IfVersion(CmpOp.Greater, 0x02000000)] public Matrix3x4 Transform;

        [IfVersion(CmpOp.Equal, 0x04000000)] private uint Hash;

        public static GfxTextureCoord Default
        {
            get
            {
                return new GfxTextureCoord()
                {
                    SourceCoordIndex = 0,
                    MappingType = GfxTextureMappingType.UvCoordinateMap,
                    ReferenceCameraIndex = 0,
                    TransformType = GfxTextureTransformType.DccMaya,
                    Scale = new Vector2(1, 1),
                    Rotation = 0,
                    Translation = new Vector2(),
                    Flags = 0,
                    Transform = new Matrix3x4(Matrix4x4.Identity),
                };
            }
        }

        public void UpdateMatrix()
        {
            Transform = CalculateMatrix(Scale, Translation, Rotation, TransformType);
        }

        public static Matrix3x4 CalculateMatrix(Vector2 scale, Vector2 translation, float rotation, GfxTextureTransformType type)
        {
            if (type == GfxTextureTransformType.DccMaya)
              return Common.TextureTransform.GetTransform(scale, rotation, translation, Common.TextureTransformType.DccMaya);
            if (type == GfxTextureTransformType.DccSoftImage)
                return Common.TextureTransform.GetTransform(scale, rotation, translation, Common.TextureTransformType.DccSoftImage);
            if (type == GfxTextureTransformType.Dcc3dsMax)
                return Common.TextureTransform.GetTransform(scale, rotation, translation, Common.TextureTransformType.Dcc3dsMax);
            return new Matrix3x4(Matrix4x4.Identity);
        }

        internal byte[] GetBytes(bool IsUninitialized)
        {
            /*
             * When the Texture Coord isn't used, Scale and Translation isn't included in the hash.
             * The reason for this is because those two are treated as reference types, even through
             * they are serialized as value types. We can't calculate the hash for a reference type
             * with equal to nullptr, so those are skipped.
             */
            using (MemoryStream MS = new MemoryStream())
            {
                BinaryWriter Writer = new BinaryWriter(MS);

                Writer.Write(SourceCoordIndex);

                Writer.Write((uint)MappingType);

                Writer.Write(ReferenceCameraIndex);

                Writer.Write((uint)TransformType);

                if (!IsUninitialized)
                {
                    Writer.Write(Scale);
                }

                Writer.Write(Rotation);

                if (!IsUninitialized)
                {
                    Writer.Write(Translation);
                }

                Writer.Write((byte)0);

                Writer.Write(Transform);

                return MS.ToArray();
            }
        }

        public void Deserialize(BinaryDeserializer Deserializer)
        {
            if(Deserializer.CurrentRevision <= 0x02000000)
            {
                UpdateMatrix(); 
                return;
            }
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            return false;
        }
    }
}
