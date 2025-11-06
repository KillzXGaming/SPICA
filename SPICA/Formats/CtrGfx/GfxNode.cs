using System.Collections.Generic;
using SPICA.Formats.Common;
using SPICA.Formats.CtrGfx.AnimGroup;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx
{
    public class GfxNode : GfxObject
    {
        public override GfxObjRevisionsV5 Revision => GfxObjRevisionsV5.Model;

        private int BranchVisible;

        [IfVersion(CmpOp.Greater, 0x02000000, true)]
        private bool _IsBranchVisible;

        public bool IsBranchVisible
        {
            get => _IsBranchVisible;
            set
            {
                _IsBranchVisible = value;

                BranchVisible = BitUtils.SetBit(BranchVisible, value, 0);
            }
        }

        public List<GfxObject> Childs;

        [IfVersion(CmpOp.Greater, 0x02000000, true)] public GfxDict<GfxAnimGroup> AnimationsGroup;
        [IfVersion(CmpOp.Lequal, 0x02000000, true)] internal uint SkipOffs;

        public GfxNode()
        {
            Childs = new List<GfxObject>();

            AnimationsGroup = new GfxDict<GfxAnimGroup>();
        }
    }
}