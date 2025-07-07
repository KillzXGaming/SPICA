
using SPICA.Formats.CtrH3D.Light;
using SPICA.PICA.Commands;
using SPICA.Rendering.SPICA_GL;

using System;
using System.Numerics;

namespace SPICA.Rendering
{
    public class Light
    {
        public Vector3 Position;
        public Vector3 Direction;
        public Vector4  Ambient;
        public Vector4 Diffuse;
        public Vector4 Specular0;
        public Vector4 Specular1;

        public LightType Type;

        public int   AngleLUTInput;
        public float AngleLUTScale;

        public float AttenuationScale;
        public float AttenuationBias;

        public string AngleLUTTableName;
        public string AngleLUTSamplerName;

        public string DistanceLUTTableName;
        public string DistanceLUTSamplerName;

        public bool Enabled;
        public bool DistAttEnabled;
        public bool TwoSidedDiffuse;
        public bool Directional;

        public Light() { }

        public Light(H3DLight Light)
        {
            OpenTK.Matrix4 Transform =
               OpenTK.Matrix4.CreateScale(Light.TransformScale.ToVector3()) *
               OpenTK.Matrix4.CreateRotationX(Light.TransformRotation.X) *
               OpenTK.Matrix4.CreateRotationY(Light.TransformRotation.Y) *
               OpenTK.Matrix4.CreateRotationZ(Light.TransformRotation.Z) *
               OpenTK.Matrix4.CreateTranslation(Light.TransformTranslation.ToVector3());

            var pos = OpenTK.Vector4.Transform(Transform, OpenTK.Vector4.UnitW).Xyz;
            Position = new Vector3(pos.X, pos.Y, pos.Z);

            Enabled = Light.IsEnabled;

            DistAttEnabled  = (Light.Flags & H3DLightFlags.HasDistanceAttenuation) != 0;
            TwoSidedDiffuse = (Light.Flags & H3DLightFlags.IsTwoSidedDiffuse)      != 0;

            AngleLUTInput = (int)Light.LUTInput;
            AngleLUTScale =      Light.LUTScale.ToSingle();

            if (Light.Content is H3DFragmentLight FragmentLight)
            {
                Direction = FragmentLight.Direction;

                Ambient   = FragmentLight.AmbientColor.ToVector4();
                Diffuse   = FragmentLight.DiffuseColor.ToVector4();
                Specular0 = FragmentLight.Specular0Color.ToVector4();
                Specular1 = FragmentLight.Specular1Color.ToVector4();

                float AttDiff =
                    FragmentLight.AttenuationEnd -
                    FragmentLight.AttenuationStart;

                AttDiff = Math.Max(AttDiff, 0.01f);

                AttenuationScale = 1f / AttDiff;
                AttenuationBias = -FragmentLight.AttenuationStart / AttDiff;

                AngleLUTTableName   = FragmentLight.AngleLUTTableName;
                AngleLUTSamplerName = FragmentLight.AngleLUTSamplerName;

                DistanceLUTTableName   = FragmentLight.DistanceLUTTableName;
                DistanceLUTSamplerName = FragmentLight.DistanceLUTSamplerName;

                Directional = Light.Type == H3DLightType.FragmentDir;

                /*
                 * Note: Directional lights doesn't need a position, because they
                 * only specify the direction the light is pointing to.
                 * The Shader expects the direction to be on the Position vector
                 * on this case. The Direction vector is only used for the SpotLight
                 * direction on the Shader.
                 */
                if (Directional)
                {
                    Position = Direction;
                }
            }
        }
    }
}
