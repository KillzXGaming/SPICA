using System.Numerics;
using Newtonsoft.Json;
using SPICA.Formats.Common;
using SPICA.Math3D;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model
{
    public class GfxBone : INamed
    {
        private string _Name;

        public string Name
        {
            get => _Name;
            set => _Name = value ?? throw Exceptions.GetNullException("Name");
        }

        public GfxBoneFlags Flags;

        public int Index;
        public int ParentIndex;

        [JsonIgnore]
        public GfxBone Parent;
        [JsonIgnore]
        public GfxBone Child;
        [JsonIgnore]
        public GfxBone PrevSibling;
        [JsonIgnore]
        public GfxBone NextSibling;

        public Vector3 Scale;
        public Vector3 Rotation;
        public Vector3 Translation;

        public Matrix3x4 LocalTransform;
        public Matrix3x4 WorldTransform;
        public Matrix3x4 InvWorldTransform;

        public GfxBillboardMode BillboardMode;

        [IfVersion(CmpOp.Greater, 0x04000000, true)]
        public GfxDict<GfxMetaData> MetaData;

        [IfVersion(CmpOp.Equal, 0x04000000, true)]
        public uint UnkA;

        public GfxBone()
        {
            MetaData = new GfxDict<GfxMetaData>();
        }

        public void UpdateMatrices()
        {
            var worldTransform = CalculateWorldMatrix();
            var localTransform = CalculateLocalMatrix();

            LocalTransform = new Matrix3x4(localTransform);
            WorldTransform = new Matrix3x4(worldTransform);

            Matrix4x4.Invert(WorldTransform, out Matrix4x4 inverse);
            InvWorldTransform = new Matrix3x4(inverse);
        }


        /// <summary>
        /// Updates the current bone transform flags.
        /// These flags determine what matrices can be ignored for matrix updating.
        /// </summary>
        public void UpdateTransformFlags()
        {
            GfxBoneFlags flags = this.Flags;

            //Reset transform flags
            flags &= ~GfxBoneFlags.IsTranslationZero;
            flags &= ~GfxBoneFlags.IsScaleVolumeOne;
            flags &= ~GfxBoneFlags.IsRotationZero;
            flags &= ~GfxBoneFlags.IsScaleUniform;

            //SRT checks to update matrices
            if (this.Translation == Vector3.Zero)
                flags |= GfxBoneFlags.IsTranslationZero;
            if (this.Scale == Vector3.One)
                flags |= GfxBoneFlags.IsScaleVolumeOne;
            if (this.Rotation == Vector3.Zero)
                flags |= GfxBoneFlags.IsRotationZero;
            //Extra scale flags
            if (this.Scale.X == this.Scale.Y && this.Scale.X == this.Scale.Z)
                flags |= GfxBoneFlags.IsScaleUniform;

            this.Flags = flags;
        }

        public Matrix4x4 CalculateWorldMatrix()
        {
            if (Parent != null)
                return CalculateLocalMatrix() * Parent.CalculateWorldMatrix();
            return CalculateLocalMatrix();
        }

        public Matrix4x4 CalculateLocalMatrix()
        {
            var trans = Matrix4x4.CreateTranslation(Translation);
            var sca = Matrix4x4.CreateScale(Scale);
            var rot = Matrix4x4.CreateRotationX(Rotation.X) *
                      Matrix4x4.CreateRotationY(Rotation.Y) *
                      Matrix4x4.CreateRotationZ(Rotation.Z);
            return sca * rot * trans;
        }
    }
}
