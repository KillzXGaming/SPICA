using System.Collections.Generic;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model.Mesh
{
    public class GfxFace
    {
        public readonly List<GfxFaceDescriptor> FaceDescriptors;

        private uint[] BufferObjs; //One for each FaceDescriptor

        [IfVersion(CmpOp.Greater, 0x04000000)]
        private uint Flags = 0;
        [IfVersion(CmpOp.Greater, 0x04000000)]
        private uint CommandAlloc = 0;

        public GfxFace()
        {
            FaceDescriptors = new List<GfxFaceDescriptor>();
            BufferObjs = new uint[1];
        }

        public void Setup()
        {
            BufferObjs = new uint[FaceDescriptors.Count];
        }
    }
}
