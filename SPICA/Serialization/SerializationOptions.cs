namespace SPICA.Serialization
{
    public struct SerializationOptions
    {
        public LengthPos LenPos;
        public PointerType PtrType;
        public bool ForceWriteStaticVersion;

        public SerializationOptions(LengthPos LenPos, PointerType PtrType, bool ForceWriteStaticVersion = false)
        {
            this.LenPos = LenPos;
            this.PtrType = PtrType;
            this.ForceWriteStaticVersion = ForceWriteStaticVersion;
        }
    }
}
