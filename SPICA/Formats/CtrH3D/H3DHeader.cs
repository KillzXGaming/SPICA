using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrH3D
{
    class H3DHeader
    {
        [Inline]
        public string Magic;

        [Version]
        public byte BackwardCompatibility;
        public byte ForwardCompatibility;

        public ushort ConverterVersion;

        public int ContentsAddress;
        public int StringsAddress;
        public int CommandsAddress;
        public int RawDataAddress;

        [IfVersion(CmpOp.Gequal, 0x21)]
        public int RawExtAddress;

        public int RelocationAddress;

        public int ContentsLength;
        public int StringsLength;
        public int CommandsLength;
        public int RawDataLength;

        [IfVersion(CmpOp.Gequal, 0x21)]
        public int RawExtLength;

        public int RelocationLength;

        public int UnInitDataLength;
#pragma warning disable CS0649 // Le champ 'H3DHeader.UnInitCommandsLength' n'est jamais assigné et aura toujours sa valeur par défaut 0
        public int UnInitCommandsLength;
#pragma warning restore CS0649 // Le champ 'H3DHeader.UnInitCommandsLength' n'est jamais assigné et aura toujours sa valeur par défaut 0

        [IfVersion(CmpOp.Gequal, 8), Padding(2)] public H3DFlags Flags;

        [IfVersion(CmpOp.Gequal, 8)] public ushort AddressCount;

        public H3DHeader()
        {
            Magic = "BCH";
        }
    }
}
