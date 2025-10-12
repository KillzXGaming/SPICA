using SPICA.Formats.Common;
using SPICA.Math3D;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SPICA.Formats.CtrGfx.Animation
{
    [JsonConverter(typeof(GfxFloatKeyFrameGroupConverter))]
    public class GfxFloatKeyFrameGroup : ICustomSerialization
    {
        private float _StartFrame;
        private float _EndFrame;

        public GfxLoopType PreRepeat;
        public GfxLoopType PostRepeat;

        private ushort Padding;

        private enum KeyFrameCurveFlags : uint
        {
            IsConstantValue  = 1 << 1,
            IsQuantizedCurve = 1 << 2
        }

        private KeyFrameCurveFlags CurveFlags;

        [JsonIgnore]
        [Ignore] public float StartFrame
        {
            get => Curves[0].StartFrame;
            set => Curves[0].StartFrame = value;
        }


        [JsonIgnore]
        [Ignore] public float EndFrame
        {
            get => Curves[0].EndFrame;
            set => Curves[0].EndFrame = value;
        }


        [JsonIgnore]
        [Ignore] public bool IsLinear
        {
            get => Curves[0].IsLinear;
            set => Curves[0].IsLinear = value;
        }

        [JsonIgnore]
        [Ignore] public KeyFrameQuantization Quantization
        {
            get => Curves[0].Quantization;
            set => Curves[0].Quantization = value;
        }

        [JsonIgnore]
        [Ignore] public List<KeyFrame> KeyFrames
        {
            get => Curves[0].KeyFrames;
            set => Curves[0].KeyFrames = value;
        }

        [Ignore] public List<Curve> Curves = new List<Curve>();

        public bool Exists => Curves.Any(x => x.KeyFrames.Count > 0);

        public GfxFloatKeyFrameGroup()
        {
            this.Curves.Add(new Curve());
        }

        void ICustomSerialization.Deserialize(BinaryDeserializer Deserializer)
        {
            if ((CurveFlags & KeyFrameCurveFlags.IsConstantValue) != 0)
            {
                float Value = Deserializer.Reader.ReadSingle();

                Curves[0].KeyFrames.Add(new KeyFrame(0, Value));
                return;
            }

            int CurveCount = Deserializer.Reader.ReadInt32();
            if (CurveCount == 0)
                return;

            var pos = Deserializer.Reader.BaseStream.Position;

            Curves.Clear();
            for (int i = 0; i < CurveCount; i++)
            {
                Curve curve = new Curve();
                Curves.Add(curve);

                Deserializer.BaseStream.Seek(pos + i * 4, SeekOrigin.Begin);
                uint pointer = Deserializer.ReadPointer();

                Deserializer.BaseStream.Seek(pointer, SeekOrigin.Begin);

                curve.StartFrame = Deserializer.Reader.ReadSingle();
                curve.EndFrame = Deserializer.Reader.ReadSingle();

                uint FormatFlags = Deserializer.Reader.ReadUInt32();
                if (FormatFlags == 1)
                {
                    float constant = Deserializer.Reader.ReadSingle();
                    curve.Quantization = KeyFrameQuantization.Constant;
                    curve.KeyFrames.Add(new KeyFrame()
                    {
                        Value = constant,
                    }); 
                    continue;
                }

                int KeysCount = Deserializer.Reader.ReadInt32();
                float InvDuration = Deserializer.Reader.ReadSingle();

                curve.Quantization = (KeyFrameQuantization)(FormatFlags >> 5);

                curve.IsLinear = (FormatFlags & 4) != 0;

                float ValueScale = 1;
                float ValueOffset = 0;
                float FrameScale = 1;

                if (curve.Quantization != KeyFrameQuantization.Hermite128 &&
                      curve.Quantization != KeyFrameQuantization.UnifiedHermite96 &&
                     curve.Quantization != KeyFrameQuantization.StepLinear64)
                {
                    ValueScale = Deserializer.Reader.ReadSingle();
                    ValueOffset = Deserializer.Reader.ReadSingle();
                    FrameScale = Deserializer.Reader.ReadSingle();
                }

                for (int Index = 0; Index < KeysCount; Index++)
                {
                    KeyFrame KF;

                    switch (curve.Quantization)
                    {
                        case KeyFrameQuantization.Hermite128: KF = Deserializer.Reader.ReadHermite128(); break;
                        case KeyFrameQuantization.Hermite64: KF = Deserializer.Reader.ReadHermite64(); break;
                        case KeyFrameQuantization.Hermite48: KF = Deserializer.Reader.ReadHermite48(); break;
                        case KeyFrameQuantization.UnifiedHermite96: KF = Deserializer.Reader.ReadUnifiedHermite96(); break;
                        case KeyFrameQuantization.UnifiedHermite48: KF = Deserializer.Reader.ReadUnifiedHermite48(); break;
                        case KeyFrameQuantization.UnifiedHermite32: KF = Deserializer.Reader.ReadUnifiedHermite32(); break;
                        case KeyFrameQuantization.StepLinear64: KF = Deserializer.Reader.ReadStepLinear64(); break;
                        case KeyFrameQuantization.StepLinear32: KF = Deserializer.Reader.ReadStepLinear32(); break;

                        default: throw new InvalidOperationException($"Invalid Segment quantization {curve.Quantization}!");
                    }

                    KF.Frame = KF.Frame * FrameScale;
                    KF.Value = KF.Value * ValueScale + ValueOffset;

                    curve.KeyFrames.Add(KF);
                }
            }
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            Serializer.Writer.Write(_StartFrame);
            Serializer.Writer.Write(_EndFrame);

            Serializer.Writer.Write((byte)PreRepeat);
            Serializer.Writer.Write((byte)PostRepeat);

            Serializer.Writer.Write(Padding);

            Serializer.Writer.Write((uint)CurveFlags);

            if (Curves[0].KeyFrames.Count < 2 && Curves.Count == 1)
            {
                if (Curves[0].KeyFrames.Count > 0)
                    Serializer.Writer.Write(Curves[0].KeyFrames[0].Value);
                else
                    Serializer.Writer.Write(0f);

                return true;
            }

            Serializer.Writer.Write(Curves.Count); //Curve Count

            var posOfs = Serializer.Writer.BaseStream.Position;
            for (int i = 0; i < Curves.Count; i++)
                Serializer.Writer.Write(0); //Curve Rel Ptr

            for (int i = 0; i < Curves.Count; i++)
            {
                // Write pointer back
                var p = Serializer.Writer.BaseStream.Position;
                var ofs = (int)(p - (posOfs + i * 4));

                Serializer.Writer.Seek((int)posOfs + i * 4, SeekOrigin.Begin);
                Serializer.Writer.Write((int)ofs);

                // Seek back
                Serializer.Writer.Seek((int)p, SeekOrigin.Begin);

                var curve = Curves[i];

                float MinFrame = curve.KeyFrames.Count > 0 ? curve.KeyFrames[0].Frame : 0;
                float MaxFrame = curve.KeyFrames.Count > 0 ? curve.KeyFrames[0].Frame : 0;
                float MinValue = curve.KeyFrames.Count > 0 ? curve.KeyFrames[0].Value : 0;
                float MaxValue = curve.KeyFrames.Count > 0 ? curve.KeyFrames[0].Value : 0;

                for (int Index = 1; Index < curve.KeyFrames.Count; Index++)
                {
                    KeyFrame KF = curve.KeyFrames[Index];

                    if (KF.Frame < MinFrame) MinFrame = KF.Frame;
                    if (KF.Frame > MaxFrame) MaxFrame = KF.Frame;
                    if (KF.Value < MinValue) MinValue = KF.Value;
                    if (KF.Value > MaxValue) MaxValue = KF.Value;
                }

                float ValueScale = KeyFrameQuantizationHelper.GetValueScale(curve.Quantization, MaxValue - MinValue);
                float FrameScale = KeyFrameQuantizationHelper.GetFrameScale(curve.Quantization, MaxFrame - MinFrame);

                float ValueOffset = MinValue;

                float InvDuration = 1f / curve.EndFrame;

                if (ValueScale == 1)
                {
                    /*
                        * Quantizations were the value scale is not needed (like the ones that already stores the value
                        * as float) will ignore the offset aswell, so we need to set to to zero.
                        */
                    ValueOffset = 0;
                }

                _StartFrame = curve.StartFrame;
                _EndFrame = curve.EndFrame;

                CurveFlags = curve.KeyFrames.Count < 2
                    ? KeyFrameCurveFlags.IsConstantValue
                    : KeyFrameCurveFlags.IsQuantizedCurve;

                uint FormatFlags = ((uint)curve.Quantization << 5) | (curve.KeyFrames.Count == 1 ? 1u : 0u);

                if (curve.Quantization >= KeyFrameQuantization.StepLinear64)
                {
                    FormatFlags |= curve.IsLinear ? 4u : 0u;
                }
                else
                {
                    FormatFlags |= 8;
                }


                Serializer.Writer.Write(curve.StartFrame);
                Serializer.Writer.Write(curve.EndFrame);
                Serializer.Writer.Write(FormatFlags);
                if (FormatFlags == 1)
                {
                    Serializer.Writer.Write(curve.KeyFrames.Count > 0 ? curve.KeyFrames[0].Value : 0.0f);
                    continue;
                }

                Serializer.Writer.Write(curve.KeyFrames.Count);
                Serializer.Writer.Write(InvDuration);

                if (curve.Quantization != KeyFrameQuantization.Hermite128 &&
                     curve.Quantization != KeyFrameQuantization.UnifiedHermite96 &&
                    curve.Quantization != KeyFrameQuantization.StepLinear64)
                {
                    Serializer.Writer.Write(ValueScale);
                    Serializer.Writer.Write(ValueOffset);
                    Serializer.Writer.Write(FrameScale);
                }

                foreach (KeyFrame Key in curve.KeyFrames)
                {
                    KeyFrame KF = Key;

                    KF.Frame = (KF.Frame / FrameScale);
                    KF.Value = (KF.Value - ValueOffset) / ValueScale;

                    switch (curve.Quantization)
                    {
                        case KeyFrameQuantization.Hermite128: Serializer.Writer.WriteHermite128(KF); break;
                        case KeyFrameQuantization.Hermite64: Serializer.Writer.WriteHermite64(KF); break;
                        case KeyFrameQuantization.Hermite48: Serializer.Writer.WriteHermite48(KF); break;
                        case KeyFrameQuantization.UnifiedHermite96: Serializer.Writer.WriteUnifiedHermite96(KF); break;
                        case KeyFrameQuantization.UnifiedHermite48: Serializer.Writer.WriteUnifiedHermite48(KF); break;
                        case KeyFrameQuantization.UnifiedHermite32: Serializer.Writer.WriteUnifiedHermite32(KF); break;
                        case KeyFrameQuantization.StepLinear64: Serializer.Writer.WriteStepLinear64(KF); break;
                        case KeyFrameQuantization.StepLinear32: Serializer.Writer.WriteStepLinear32(KF); break;
                    }
                }

                while ((Serializer.BaseStream.Position & 3) != 0) Serializer.BaseStream.WriteByte(0);
            }

            return true;
        }

        internal static GfxFloatKeyFrameGroup ReadGroup(BinaryDeserializer Deserializer, bool Constant)
        {
            GfxFloatKeyFrameGroup FrameGrp = new GfxFloatKeyFrameGroup();

            if (Constant)
            {
                FrameGrp.KeyFrames.Add(new KeyFrame(0, Deserializer.Reader.ReadSingle()));
            }
            else
            {
                uint Address = Deserializer.ReadPointer();

                Deserializer.BaseStream.Seek(Address, SeekOrigin.Begin);

                FrameGrp = Deserializer.Deserialize<GfxFloatKeyFrameGroup>();
            }

            return FrameGrp;
        }

        public class Curve
        {
            [Ignore] public float StartFrame;
            [Ignore] public float EndFrame;

            [Ignore] public bool IsLinear;

            [Ignore] public KeyFrameQuantization Quantization;

            [Ignore] public List<KeyFrame> KeyFrames = new List<KeyFrame>();
        }


        // Temp fix. We make curve data on construct with the way this group is interacted with
        // But json adds this extra curve we do not want
        public class GfxFloatKeyFrameGroupConverter : JsonConverter<GfxFloatKeyFrameGroup>
        {
            public override GfxFloatKeyFrameGroup ReadJson(JsonReader reader, Type objectType, GfxFloatKeyFrameGroup existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                JObject obj = JObject.Load(reader);

                existingValue.Curves = obj["Curves"]?.ToObject<List<Curve>>(serializer) ?? new List<Curve>();

                return existingValue;
            }

            public override void WriteJson(JsonWriter writer, GfxFloatKeyFrameGroup value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, value);
            }
        }
    }
}
