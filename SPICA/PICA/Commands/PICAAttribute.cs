using System.Collections.Generic;

namespace SPICA.PICA.Commands
{
    public struct PICAAttribute
    {
        private const float SCALE_SHORT_UNIT = 1f / 32767f; //shorts are signed
        private const float SCALE_UBYTE_UNIT = 1f / 255f;

        public PICAAttributeName Name;
        public PICAAttributeFormat Format;
        public int Elements;
        public float Scale;

        public static List<PICAAttribute> GetAttributes(params PICAAttributeName[] Names)
        {
            List<PICAAttribute> Output = new List<PICAAttribute>();

            foreach (PICAAttributeName Name in Names)
            {
                switch (Name)
                {
                    case PICAAttributeName.Position:
                    case PICAAttributeName.Normal:
                    case PICAAttributeName.Tangent:
                        Output.Add(new PICAAttribute()
                        {
                            Name = Name,
                            Format = PICAAttributeFormat.Float,
                            Elements = 3,
                            Scale = 1
                        });
                        break;

                    case PICAAttributeName.TexCoord0:
                    case PICAAttributeName.TexCoord1:
                    case PICAAttributeName.TexCoord2:
                        Output.Add(new PICAAttribute()
                        {
                            Name = Name,
                            Format = PICAAttributeFormat.Float,
                            Elements = 2,
                            Scale = 1
                        });
                        break;

                    case PICAAttributeName.Color:
                        Output.Add(new PICAAttribute()
                        {
                            Name = PICAAttributeName.Color,
                            Format = PICAAttributeFormat.Ubyte,
                            Elements = 4,
                            Scale = SCALE_UBYTE_UNIT
                        });
                        break;

                    case PICAAttributeName.BoneIndex:
                        Output.Add(new PICAAttribute()
                        {
                            Name = PICAAttributeName.BoneIndex,
                            Format = PICAAttributeFormat.Ubyte,
                            Elements = 4,
                            Scale = 1
                        });
                        break;

                    case PICAAttributeName.BoneWeight:
                        Output.Add(new PICAAttribute()
                        {
                            Name = PICAAttributeName.BoneWeight,
                            Format = PICAAttributeFormat.Short,
                            //Some higher quality models (Pokemon SwSh) get severely downgraded by the lack of precision on UBytes (yes, even the 1/255 makes a difference apparently)
                            //Writing as full floats would be a waste of space, so we use int16s instead
                            Elements = 4,
                            Scale = SCALE_SHORT_UNIT
                        });
                        break;
                }
            }

            return Output;
        }
    }
}
