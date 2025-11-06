using System;

namespace SPICA.Serialization.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    class IfVersionAttribute : Attribute
    {
        //(M-1)TODO: Enum to check any section (if it's already been read)
        //Is it even worth the effort? lol

        public CmpOp Comparer;
        public int   Version;
        public bool CompareMainBinary = false;
        //public bool ComparePrevSection = false;

        public IfVersionAttribute(CmpOp Comparer, int Version, bool CompareMainBinary = false)//, bool comparePreviousSection = false)
        {
            this.Comparer = Comparer;
            this.Version  = Version;
            this.CompareMainBinary = CompareMainBinary;
            //this.ComparePrevSection = comparePreviousSection;
        }

        public bool Compare(uint SectionVersion, uint MainFileVersion = 0)
        {
            var Version = CompareMainBinary ? MainFileVersion : SectionVersion;
            switch (Comparer)
            {
                case CmpOp.Equal:   return Version == this.Version;
                case CmpOp.Notqual: return Version != this.Version;
                case CmpOp.Greater: return Version >  this.Version;
                case CmpOp.Gequal:  return Version >= this.Version;
                case CmpOp.Less:    return Version <  this.Version;
                case CmpOp.Lequal:  return Version <= this.Version;

                default: throw new InvalidOperationException($"Invalid comparison operator {Comparer}!");
            }
        }
    }
}
