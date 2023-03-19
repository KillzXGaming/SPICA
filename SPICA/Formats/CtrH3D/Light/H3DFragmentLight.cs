using SPICA.Math3D;

using System.Numerics;

namespace SPICA.Formats.CtrH3D.Light
{
    public class H3DFragmentLight
    {
        public RGBA AmbientColor;
        public RGBA DiffuseColor;
        public RGBA Specular0Color;
        public RGBA Specular1Color;

        public Vector3 Direction;

#pragma warning disable CS0169 // Le champ 'H3DFragmentLight.DistanceSamplerPtr' n'est jamais utilisé
        private uint DistanceSamplerPtr;
#pragma warning restore CS0169 // Le champ 'H3DFragmentLight.DistanceSamplerPtr' n'est jamais utilisé
#pragma warning disable CS0169 // Le champ 'H3DFragmentLight.AngleSamplerPtr' n'est jamais utilisé
        private uint AngleSamplerPtr;
#pragma warning restore CS0169 // Le champ 'H3DFragmentLight.AngleSamplerPtr' n'est jamais utilisé

        public float AttenuationStart;
        public float AttenuationEnd;

        public string DistanceLUTTableName;
        public string DistanceLUTSamplerName;

        public string AngleLUTTableName;
        public string AngleLUTSamplerName;
    }
}
