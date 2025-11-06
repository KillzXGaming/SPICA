using SPICA.Formats.Common;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrGfx.Model;
using SPICA.Formats.CtrGfx.Model.Mesh;
using SPICA.Formats.GFL2.Model;
using SPICA.Math3D;
using SPICA.Serialization.Attributes;
using SPICA.Serialization.Serializer;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters;
using System.Text;

namespace SPICA.Serialization
{
    class BinarySerializer : BinarySerialization
    {
        public readonly BinaryWriter Writer;

        private struct ObjectInfo
        {
            public uint Position;
            public int  Length;
        }

        private const uint MainSection = 0;

        private Dictionary<Type, Section> TypeSections;

        private Dictionary<object, ObjectInfo> ObjPointers;

        public readonly SortedDictionary<uint, Section> Sections;

        public readonly List<long> Pointers;

        private bool IsGfx = false;

        public BinarySerializer(Stream BaseStream, SerializationOptions Options, bool isGfx = false) : base(BaseStream, Options)
        {
            IsGfx = isGfx;
            Writer = new BinaryWriter(BaseStream);

            TypeSections = new Dictionary<Type, Section>();

            ObjPointers = new Dictionary<object, ObjectInfo>();

            Sections = new SortedDictionary<uint, Section>();

            Pointers = new List<long>();

            Sections.Add(MainSection, new Section());
        }

        public void AddSection(uint SectionId, Section Section, Type Type)
        {
            TypeSections.Add(Type, Section);

            if (!Sections.ContainsKey(SectionId))
            {
                Sections.Add(SectionId, Section);
            }
        }

        public void AddSection(uint SectionId, Section Section)
        {
            Sections.Add(SectionId, Section);
        }

        public void Serialize(object Value)
        {
            Sections[MainSection].Values.Add(new RefValue(Value));

            foreach (KeyValuePair<uint, Section> KV in Sections)
            {
                WriteSection(KV.Key, KV.Value);
            }
        }

        private void WriteSection(uint sectionID, Section Section)
        {
            //Sort
            if (Section.Comparer != null)
            {
                Section.Values.RemoveAll(x => x.Value == null);
                Section.Values.Sort(Section.Comparer);
            }

            //Write
            long HeaderPosition = BaseStream.Position;

            if (Section.Header != null)
            {
                WriteValue(Section.Header);
            }

            Section.Position = (int)BaseStream.Position;

            WriteSection(Section.Values);

            //String table. Align for the image block
            if (IsGfx && (GfxSectionId)sectionID == GfxSectionId.Strings)
            {
                int alignment = 128;
                var size = (-(int)(Writer.BaseStream.Position + 8) % alignment + alignment) % alignment;

                Writer.Write(new byte[size]);
            }

            //Set section position and lengths, where:
            //Length is the data length, and length with header is
            //data length + header length.
            //Position is the header position (or data position if it doesn't have a header).
            Section.Length           = (int)(BaseStream.Position - Section.Position);
            Section.LengthWithHeader = (int)(BaseStream.Position - HeaderPosition);
            Section.HeaderLength     = (int)(Section.Position    - HeaderPosition);

            Align(Section.Padding);
        }

        public void AlignBytes(BinaryWriter writer, int alignment, byte value = 0x00)
        {
            var startPos = writer.BaseStream.Position;
            long position = writer.Seek((-(int)writer.BaseStream.Position % alignment + alignment) % alignment, SeekOrigin.Current);

            writer.Seek((int)startPos, System.IO.SeekOrigin.Begin);
            while (writer.BaseStream.Position != position)
            {
                writer.Write(value);
            }
        }

        private RefValue CurrentValue;

        private void WriteSection(List<RefValue> Values, int Start = 0)
        {
            //Write list offsets first
            WriteListOffsets(Values, Start);

            for (int Index = Start; Index < Values.Count; Index++)
            {
                //Skip list offsets as they have been written 
                if (Values[Index].Info != null && IsList(Values[Index].Info.FieldType))
                    continue;

                CurrentValue = Values[Index];
                WriteValue(Values[Index]);
            }

            int LastIndex = Values.Count;

            for (int Index = Start; Index < Values.Count; Index++)
            {
                WriteSection(Values[Index].Childs, 0);
            }

            if (Values.Count > LastIndex)
            {
                WriteSection(Values, LastIndex);
            }
        }

        private void WriteListOffsets(List<RefValue> Values, int Start = 0, string ind = "")
        {
            for (int Index = Start; Index < Values.Count; Index++)
            {
                if (Values[Index].Info != null && IsList(Values[Index].Info.FieldType))
                {
                    CurrentValue = Values[Index];
                    WriteValue(Values[Index]);
                }
            }
        }

        public void WriteValue(object Value)
        {
            Type Type = Value.GetType();

            long Position = BaseStream.Position;

            if (Type.IsPrimitive || Type.IsEnum)
            {
                switch (Type.GetTypeCode(Type))
                {
                    case TypeCode.UInt64:  Writer.Write((ulong)Value);          break;
                    case TypeCode.UInt32:  Writer.Write((uint)Value);           break;
                    case TypeCode.UInt16:  Writer.Write((ushort)Value);         break;
                    case TypeCode.Byte:    Writer.Write((byte)Value);           break;
                    case TypeCode.Int64:   Writer.Write((long)Value);           break;
                    case TypeCode.Int32:   Writer.Write((int)Value);            break;
                    case TypeCode.Int16:   Writer.Write((short)Value);          break;
                    case TypeCode.SByte:   Writer.Write((sbyte)Value);          break;
                    case TypeCode.Single:  Writer.Write((float)Value);          break;
                    case TypeCode.Double:  Writer.Write((double)Value);         break;
                    case TypeCode.Boolean: Writer.Write((bool)Value ? 1u : 0u); break;
                }
            }
            else if (Value is IList)
            {
                WriteList((IList)Value);
            }
            else if (Value is string)
            {
                Writer.Write(Encoding.ASCII.GetBytes((string)Value + '\0'));
            }
            else if (Value is Vector2)
            {
                Writer.Write((Vector2)Value);
            }
            else if (Value is Vector3)
            {
                Writer.Write((Vector3)Value);
            }
            else if (Value is Vector4)
            {
                Writer.Write((Vector4)Value);
            }
            else if (Value is Quaternion)
            {
                Writer.Write((Quaternion)Value);
            }
            else if (Value is Matrix3x3)
            {
                Writer.Write((Matrix3x3)Value);
            }
            else if (Value is Matrix3x4)
            {
                Writer.Write((Matrix3x4)Value);
            }
            else if (Value is Matrix4x4)
            {
                Writer.Write((Matrix4x4)Value);
            }
            else
            {
                WriteObject(Value);
            }

            if (Value is byte[])
                AddObjInfo(Value, Position);

            //Avoid writing the same Object more than once
            if (Type.IsClass) AddObjInfo(Value, Position);


            // M-1: HACK! Make sure vertex data is aligned
            // Should we do the same with face indices?
            if (Value is byte[] bytes)
            {
                if (CurrentValue?.Parent is GfxAttribute || 
                    CurrentValue?.Parent is GfxVertexBufferInterleaved)
                {
                    AlignBytes(Writer, 4);
                }
            }
        }

        private void AddObjInfo(object Value, long Position)
        {
            if (Value is byte[])
            {
                if (ObjPointers.Keys.Any(x => (x is byte[] && ((byte[])x).SequenceEqual((byte[])Value))))
                    return;
            }

            if (!ObjPointers.ContainsKey(Value))
            {
                ObjPointers.Add(Value, new ObjectInfo()
                {
                    Position = (uint)Position,
                    Length   = (int)(BaseStream.Position - Position)
                });
            }
        }

        private void WriteList(IList List)
        {
            if (List.Count == 0) return;

            Type Type = List.GetType();

            Type = Type.IsArray
                ? Type.GetElementType()
                : Type.GetGenericArguments()[0];

            bool IsBool  = Type == typeof(bool);
            bool Inline  = Type.IsDefined(typeof(InlineAttribute));
            bool IsValue = Type.IsValueType || Type.IsEnum || Inline;

            BitWriter BW = new BitWriter(Writer);

            foreach (object Value in List)
            {
                if (!IsValue)
                {
                    RefValue Ref = new RefValue()
                    {
                        Value     = Value,
                        Position  = BaseStream.Position,
                        HasLength = IsList(Type)
                    };

                    AddReference(Type, Ref);

                    BaseStream.Seek(Ref.HasLength ? 8 : 4, SeekOrigin.Current);
                }
                else if (IsBool)
                {
                    BW.WriteBit((bool)Value);
                }
                else
                {
                    WriteValue(Value);
                }
            }

            BW.Flush();
        }

        private void WriteValue(RefValue Ref)
        {
            FieldInfo Info   = Ref.Info;
            object    Parent = Ref.Parent;
            object    Value  = Ref.Value;
            bool      Range  = Info?.IsDefined(typeof(RangeAttribute)) ?? false;
            LengthPos LenPos = GetLengthPos(Info);

            if (Ref.Padding != 0)
                Align(Ref.Padding);

            if (Value != null && (!(Value is IList) || ((IList)Value).Count > 0 || Range))
            {
                ObjectInfo ObjInfo = GetObjInfo(Value, Info);

                long Position = BaseStream.Position;

                if (ObjInfo.Position == Position)
                {
                    if (Parent != null &&
                        Parent is ICustomSerializeCmd &&
                        Info?.FieldType == typeof(uint[]))
                    {
                        ((ICustomSerializeCmd)Parent).SerializeCmd(this, Value);
                    }

                    AddObjInfo(Value, Position);
                    WriteValue(Value);
                }

                if (Ref.Position != -1)
                {
                    long EndPos = BaseStream.Position;

                    BaseStream.Seek(Ref.Position, SeekOrigin.Begin);

                    uint Pointer = ObjInfo.Position + Ref.PointerOffset;

                    if (LenPos == LengthPos.AfterPtr)
                    {
                        WritePointer(Pointer);
                    }

                    if (Ref.HasLength)
                    {
                        if (Range)
                        {
                            WritePointer((uint)(ObjInfo.Length != 0 ? ObjInfo.Length : EndPos));
                        }
                        else if (GetLengthSize(Info) == LengthSize.Short)
                        {
                            Writer.Write((ushort)((IList)Value).Count);
                        }
                        else
                        {
                            Writer.Write(((IList)Value).Count);
                        }
                    }

                    if (LenPos == LengthPos.BeforePtr)
                    {
                        WritePointer(Pointer);
                    }

                    if (Ref.HasTwoPtr)
                    {
                        WritePointer(Pointer);
                    }

                    BaseStream.Seek(EndPos, SeekOrigin.Begin);
                }
            }
        }

        public void WritePointer(uint Pointer)
        {
            Pointers.Add(BaseStream.Position);

            if (Options.PtrType == PointerType.SelfRelative && Pointer != 0)
            {
                Pointer -= (uint)BaseStream.Position;
            }

            Writer.Write(Pointer);
        }

        private ObjectInfo GetObjInfo(object Value, FieldInfo Info)
        {
            ObjectInfo Output = new ObjectInfo()
            {
                Position = (uint)BaseStream.Position,
                Length   = 0
            };

            //A fix for shader data. Check if byte[] exists in the pointer list, then point to that instead
            //A shader can point to the same shader data
            if (Info?.Name == "Program" && Value is byte[])
            {
                var existing = ObjPointers.Where(x => (x.Key is byte[] && ((byte[])x.Key).SequenceEqual((byte[])Value))).ToList();
                if (existing.Count > 0)
                    Output = existing[0].Value;
            }
            else if (ObjPointers.ContainsKey(Value))
            {
                Output = ObjPointers[Value];
            }
            else if (Value is IList)
            {
                //This is used to find lists with segments of already serialized values.
                //We can avoid storing them again if the same sequence is repeated.
                uint StartPos = 0;
                int  EndPos   = 0;
                int  Matches  = 0;

                foreach (object Elem in ((IList)Value))
                {
                    if (ObjPointers.ContainsKey(Elem) && (
                        EndPos == ObjPointers[Elem].Position ||
                        EndPos == 0))
                    {
                        if (Matches++ == 0)
                        {
                            EndPos = (int)(StartPos = ObjPointers[Elem].Position);
                        }
                    }
                    else
                    {
                        break;
                    }

                    EndPos += ObjPointers[Elem].Length;
                }

                if (Matches > 0 && Matches == ((IList)Value).Count)
                {
                    Output.Position = StartPos;
                    Output.Length   = EndPos;
                }
            }

            return Output;
        }

        private void WriteObject(object Value)
        {
            //M-1: Hack to update Revision to the latest version of bcres, since that's what we support saving for
            if (Value is GfxObject gfx)
            {
                gfx.Header.Revision = (uint)gfx.Revision;
            }
            Type ValueType = Value.GetType();

            if (ValueType.IsDefined(typeof(TypeChoiceAttribute)))
            {
                foreach (TypeChoiceAttribute Attr in ValueType.GetCustomAttributes<TypeChoiceAttribute>())
                {
                    if (Attr.Type == ValueType)
                    {
                        //M-1: Hack for converting older versions to new versions
                        Writer.Write(Attr.WriteNewestType ? Attr.NewTypeVal : Attr.TypeVal);

                        break;
                    }
                }
            }

            if (Value is ICustomSerialization)
            {
                if (((ICustomSerialization)Value).Serialize(this)) return;
            }

            Console.WriteLine($"{Value}");

            foreach (FieldInfo Info in GetFieldsSorted(ValueType))
            {
                if (!Info.GetCustomAttribute<IfVersionAttribute>()?.Compare(CurrentRevision, MainFileVersion) ?? false) continue;

                if (!(
                    Info.IsDefined(typeof(IgnoreAttribute)) ||
                    Info.IsDefined(typeof(CompilerGeneratedAttribute))))
                {
                    object FieldValue = Info.GetValue(Value);

                    Type Type = Info.FieldType;

                    bool Inline;

                    Inline  = Info.IsDefined(typeof(InlineAttribute));
                    Inline |= Type.IsDefined(typeof(InlineAttribute));

                    if (Type.IsValueType || Type.IsEnum || Inline)
                    {
                        if (Type.IsPrimitive && Info.IsDefined(typeof(VersionAttribute)))
                        {

                            if (Options.ForceWriteStaticVersion)
                            {
                                FieldValue = Convert.ChangeType(MainFileVersion, Type);
                            }
                            else
                            {
                                RevisionStack.Push(Convert.ToUInt32(FieldValue));
                            }
                        }

                        if (IsList(Type) && !Info.IsDefined(typeof(FixedLengthAttribute)))
                        {
                            Writer.Write(((IList)FieldValue).Count);
                        }

                        WriteValue(FieldValue);
                    }
                    else
                    {
                        bool HasLength = !Info.IsDefined(typeof(FixedLengthAttribute)) && IsList(Type);
                        bool HasTwoPtr = Info.IsDefined(typeof(RepeatPointerAttribute));

                        RefValue Ref = new RefValue()
                        {
                            Parent    = Value,
                            Info      = Info,
                            Value     = FieldValue,
                            Position  = BaseStream.Position,
                            HasLength = HasLength,
                            HasTwoPtr = HasTwoPtr
                        };

                        AddReference(Type, Ref);

                        int LenSize = HasLength ? GetIntLengthSize(Info) : 0;

                        BaseStream.Seek(4 + LenSize + (HasTwoPtr ? 4 : 0), SeekOrigin.Current);
                    }

                    Align(Info.GetCustomAttribute<PaddingAttribute>()?.Size ?? 1);
                }
            }

            if (typeof(GfxObject).IsAssignableFrom(ValueType))
            {
                RevisionStack.Pop();
            }
        }

        internal void AddReference(Type Type, RefValue Ref)
        {
            if (Ref.Info?.IsDefined(typeof(SectionAttribute)) ?? false)
            {
                Ref.Padding = Ref.Info.GetCustomAttribute<SectionAttribute>().Padding;
                Sections[Ref.Info.GetCustomAttribute<SectionAttribute>().SectionId].Values.Add(Ref);
            }
            else if (TypeSections.ContainsKey(Type))
            {
                TypeSections[Type].Values.Add(Ref);
            }
            else
            {
                CurrentValue.Childs.Add(Ref);
            }
        }
    }
}
