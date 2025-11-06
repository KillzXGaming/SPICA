using SPICA.Formats.Common;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Model
{
    [TypeChoice(0x02000000u, typeof(GfxSkeleton))]
    [TypeChoice(0x00000040u, typeof(GfxSkeleton))]
    public class GfxSkeleton : GfxObject, INamed
    {
        public override GfxObjRevisionsV5 Revision => GfxObjRevisionsV5.Skeleton;

        public readonly GfxDict<GfxBone> Bones;

        public GfxBone RootBone;

        public GfxSkeletonScalingRule ScalingRule;

        [IfVersion(CmpOp.Greater, 0x02000000, true)] private int Flags;

        public bool IsTranslationAnimEnabled
        {
            get => BitUtils.GetBit(Flags, 1);
            set => Flags = BitUtils.SetBit(Flags, value, 1);
        }

        public GfxSkeleton()
        {
            Bones = new GfxDict<GfxBone>();
            this.Header.MagicNumber = 0x4A424F53;
        }
    }
}
