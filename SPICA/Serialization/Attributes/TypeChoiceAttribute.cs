using System;
using SPICA.Formats.CtrGfx;

namespace SPICA.Serialization.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    class TypeChoiceAttribute : Attribute
    {
        public uint TypeVal;
        public Type Type;

        public bool WriteNewestType;
        public uint NewTypeVal;

        public TypeChoiceAttribute(uint TypeVal, Type Type, GfxObjTypesV5 NewTypeVal = GfxObjTypesV5.None)
        {
            this.TypeVal = TypeVal;
            this.Type    = Type;

            this.WriteNewestType = NewTypeVal != GfxObjTypesV5.None;
            this.NewTypeVal = (uint)NewTypeVal;
        }
    }
}
