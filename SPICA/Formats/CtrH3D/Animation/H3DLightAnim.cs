using SPICA.Formats.CtrH3D.Camera;
using SPICA.Formats.CtrH3D.Light;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;
using System.Collections.Generic;

namespace SPICA.Formats.CtrH3D.Animation
{
    public class H3DLightAnim : H3DAnimation, ICustomSerialization
    {
        public H3DLightType LightType;

        [Inline, IfVersion(CmpOp.Greater, 0x21), FixedLength(12), Padding(4)]
        public sbyte[] ElementIndicesV2;

        [Inline, IfVersion(CmpOp.Lequal, 0x21), FixedLength(9), Padding(4)]
        public sbyte[] ElementIndicesV1;

        public H3DLightAnim()
        {
        }

        public H3DLightAnim(H3DAnimation Anim) : this()
        {
            Name = Anim.Name;

            AnimationFlags = Anim.AnimationFlags;
            AnimationType  = Anim.AnimationType;

            CurvesCount = Anim.CurvesCount;

            FramesCount = Anim.FramesCount;

            Elements.AddRange(Anim.Elements);

            MetaData = Anim.MetaData;
        }

        public void Deserialize(BinaryDeserializer Deserializer)
        {
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            if (Serializer.CurrentRevision > 0x21)
                ElementIndicesV2 = CalculateElementIndicesV2();
            else
                ElementIndicesV1 = CalculateElementIndicesV1();

            return false;
        }

        private sbyte[] CalculateElementIndicesV1()
        {
            sbyte[] indices = new sbyte[10];
            for (int i = 0; i < indices.Length; i++)
                indices[i] = -1;

            for (int i = 0; i < this.Elements.Count; i++)
            {
                var target = this.Elements[i].TargetType;
                //Version does not have these enums, shift downwards
                if (target >= H3DTargetType.LightGround) target -= 1;
                if (target >= H3DTargetType.LightSky) target -= 1;
                if (target >= H3DTargetType.LightInterpolationFactor) target -= 1;

                var type = (int)(target - H3DTargetType.LightTransform);
                indices[type] = (sbyte)i;
            }
            return indices;
        }

        private sbyte[] CalculateElementIndicesV2()
        {
            sbyte[] indices = new sbyte[10];
            for (int i = 0; i < indices.Length; i++)
                indices[i] = -1;

            for (int i = 0; i < this.Elements.Count; i++)
            {
                var type = (int)(this.Elements[i].TargetType - H3DTargetType.LightTransform);
                indices[type] = (sbyte)i;
            }
            return indices;
        }
    }
}
