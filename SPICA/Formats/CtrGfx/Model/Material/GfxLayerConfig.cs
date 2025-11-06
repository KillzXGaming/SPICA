using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPICA.Formats.CtrGfx.Model.Material
{
    public enum GfxLayerConfig
    {
        LayerConfig0, // Spot_Dist0_RefR               | 1 Cycle
        LayerConfig1, // Spot_Frsnl_RefR               | 1 Cycle
        LayerConfig2, // Dist0_Dist1_RefR              | 1 Cycle
        LayerConfig3, // Dist0_Dist1_Frsnl             | 1 Cycle
        LayerConfig4, // Spot_Dist0_Dist1_RefRGB       | 3 Cycle
        LayerConfig5, // Spot_Dist0_RefRGB_Frsnl       | 3 Cycle
        LayerConfig6, // Spot_Dist0_Dist1_RefR_Frsnl   | 3 Cycle
        LayerConfig7, // Spot_Dist0_Dist1_RefRGB_Frsnl | 4 Cycle
        LayerConfig8  // Same as 7
    }
}
