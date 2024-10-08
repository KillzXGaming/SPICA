﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SPICA.PICA.Commands
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PICATextureCombinerSource
    {
        PrimaryColor = 0,
        FragmentPrimaryColor = 1,
        FragmentSecondaryColor = 2,
        Texture0 = 3,
        Texture1 = 4,
        Texture2 = 5,
        Texture3 = 6,
        PreviousBuffer = 13,
        Constant = 14,
        Previous = 15
    }
}
