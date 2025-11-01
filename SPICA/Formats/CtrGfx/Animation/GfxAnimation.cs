using SPICA.Formats.Common;
using SPICA.Formats.CtrGfx.AnimGroup;
using SPICA.Formats.CtrH3D.Animation;

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SPICA.Formats.CtrGfx.Animation
{
    public class GfxAnimation : INamed
    {
        private GfxRevHeader Header;

        private string _Name;

        public string Name
        {
            get => _Name;
            set => _Name = value ?? throw Exceptions.GetNullException("Name");
        }

        public string TargetAnimGroupName = "";

        public GfxLoopMode LoopMode;

        public float FramesCount;

        public GfxDict<GfxAnimationElement> Elements;

        public GfxDict<GfxMetaData> MetaData;

        private const string MatCoordScaleREx = @"Materials\[""(.+)""\]\.TextureCoordinators\[(\d)\]\.Scale";
        private const string MatCoordRotREx   = @"Materials\[""(.+)""\]\.TextureCoordinators\[(\d)\]\.Rotate";
        private const string MatCoordTransREx = @"Materials\[""(.+)""\]\.TextureCoordinators\[(\d)\]\.Translate";
        private const string MatColorREx      = @"Materials\[""(.+)""\]\.MaterialColor\.(\w+)";
        private const string MatMapperBCREx   = @"Materials\[""(.+)""\]\.TextureMappers\[(\d)\]\.Sampler\.BorderColor";
        private const string MatMapperTexREx = @"Materials\[""(.+)""\]\.TextureMappers\[(\d)\]\.Texture";
        private const string MeshNodeVisREx   = @"MeshNodeVisibilities\[""(.+)""\]\.IsVisible";
        private const string MeshVisREx = @"Meshes\[(\d)\]\.IsVisible";

        private const string ViewUpdaterTarget = "ViewUpdater.TargetPosition";
        private const string ViewUpdaterUpVec  = "ViewUpdater.UpwardVector";
        private const string ViewUpdaterRotate = "ViewUpdater.ViewRotate";
        private const string ViewUpdaterTwist  = "ViewUpdater.Twist";

        private const string ProjectionUpdaterNear = "ProjectionUpdater.Near";
        private const string ProjectionUpdaterFar  = "ProjectionUpdater.Far";
        private const string ProjectionUpdaterFOVY = "ProjectionUpdater.Fovy";
        private const string ProjectionUpdaterHeight = "ProjectionUpdater.Height";
        private const string ProjectionUpdaterAspect = "ProjectionUpdater.Aspect";

        private const string LightDistanceAttenuationStart = "DistanceAttenuationStart";
        private const string LightDistanceAttenuationEnd = "DistanceAttenuationEnd";

        private const string LightLerpFactor = "LerpFactor";

        public GfxAnimation()
        {
            Elements = new GfxDict<GfxAnimationElement>();
            MetaData = new GfxDict<GfxMetaData>();
            this.Header.MagicNumber = 0x4D4E4143;
            this.Header.Revision = 117440512;
            this.LoopMode = GfxLoopMode.Loop;
        }

        public H3DAnimation ToH3DAnimation()
        {
            H3DAnimation Output = new H3DAnimation()
            {
                Name           = _Name,
                FramesCount    = FramesCount,
                AnimationFlags = (H3DAnimationFlags)LoopMode
            };
            if (TargetAnimGroupName == "MaterialAnimation")
            {
                Output = new H3DMaterialAnim()
                {
                    Name = _Name,
                    FramesCount = FramesCount,
                    AnimationFlags = (H3DAnimationFlags)LoopMode
                };
            }
            if (TargetAnimGroupName == "CameraAnimation")
            {
                Output = new H3DCameraAnim()
                {
                    Name = _Name,
                    FramesCount = FramesCount,
                    AnimationFlags = (H3DAnimationFlags)LoopMode
                };
            }
            

            switch (TargetAnimGroupName)
            {
                case "SkeletalAnimation":   Output.AnimationType = H3DAnimationType.Skeletal;   break;
                case "MaterialAnimation":   Output.AnimationType = H3DAnimationType.Material;   break;
                case "VisibilityAnimation": Output.AnimationType = H3DAnimationType.Visibility; break;
                case "LightAnimation":      Output.AnimationType = H3DAnimationType.Light;      break;
                case "CameraAnimation":     Output.AnimationType = H3DAnimationType.Camera;     break;
                case "FogAnimation":        Output.AnimationType = H3DAnimationType.Fog;        break;
            }

            foreach (GfxAnimationElement Elem in Elements)
            {
                switch (Elem.PrimitiveType)
            	{
                    case GfxPrimitiveType.Float:
                        {
                            H3DAnimFloat Float = new H3DAnimFloat();

                            CopyKeyFrames(((GfxAnimFloat)Elem.Content).Value, Float.Value);

                            H3DTargetType TargetType = 0;

                            string Name = Elem.Name;

                            switch (Name)
                            {
                                case ProjectionUpdaterNear: TargetType = H3DTargetType.CameraZNear; break;
                                case ProjectionUpdaterFar: TargetType = H3DTargetType.CameraZFar; break;
                                case ProjectionUpdaterHeight: TargetType = H3DTargetType.CameraHeight; break;
                                case ProjectionUpdaterFOVY: TargetType = H3DTargetType.CameraFovY; break;
                                case ProjectionUpdaterAspect: TargetType = H3DTargetType.CameraAspectRatio; break;
                                case ViewUpdaterTwist: TargetType = H3DTargetType.CameraTwist; break;
                                case LightDistanceAttenuationStart: TargetType = H3DTargetType.LightAttenuationStart; break;
                                case LightDistanceAttenuationEnd: TargetType = H3DTargetType.LightAttenuationEnd; break;
                                case LightLerpFactor: TargetType = H3DTargetType.LightInterpolationFactor; break;
                                default:
                                    Match Path = Regex.Match(Elem.Name, MatCoordRotREx);

                                    if (Path.Success && int.TryParse(Path.Groups[2].Value, out int CoordIdx))
                                    {
                                        Name = Path.Groups[1].Value;

                                        switch (CoordIdx)
                                        {
                                            case 0: TargetType = H3DTargetType.MaterialTexCoord0Rot; break;
                                            case 1: TargetType = H3DTargetType.MaterialTexCoord1Rot; break;
                                            case 2: TargetType = H3DTargetType.MaterialTexCoord2Rot; break;
                                        }
                                    }
                                    break;
                            }

                            if (TargetType != 0)
                            {
                                Output.Elements.Add(new H3DAnimationElement()
                                {
                                    Name          = Name,
                                    Content       = Float,
                                    PrimitiveType = H3DPrimitiveType.Float,
                                    TargetType    = TargetType
                                });
                            }
                        }
                        break;

                    case GfxPrimitiveType.Boolean:
                        {
                            H3DAnimBoolean Bool = new H3DAnimBoolean();

                            GfxAnimBoolean Source = (GfxAnimBoolean)Elem.Content;

                            Bool.StartFrame = Source.StartFrame;
                            Bool.EndFrame   = Source.EndFrame;

                            Bool.PreRepeat  = (H3DLoopType)Source.PreRepeat;
                            Bool.PostRepeat = (H3DLoopType)Source.PostRepeat;

                            CopyList(Source.Values, Bool.Values);

                            H3DTargetType TargetType = 0;

                            Match Path = Regex.Match(Elem.Name, MeshNodeVisREx);
                            Match PathVis = Regex.Match(Elem.Name, MeshVisREx);

                            if (Path.Success)
                            {
                                TargetType = H3DTargetType.MeshNodeVisibility;
                            }
                            else if (PathVis.Success)
                            {
                                TargetType = H3DTargetType.MeshIndexNodeVisibility;
                            }

                            if ((PathVis.Success || Path.Success) && TargetType != 0)
                            {
                                string Name = PathVis.Success ? $"Meshes[{PathVis.Groups[1].Value}]" : Path.Groups[1].Value;

                                Output.Elements.Add(new H3DAnimationElement()
                                {
                                    Name          = Name,
                                    Content       = Bool,
                                    PrimitiveType = H3DPrimitiveType.Boolean,
                                    TargetType    = TargetType
                                });
                            }
                        }
                        break;

                    case GfxPrimitiveType.Vector2D:
                        {
                            H3DAnimVector2D Vector = new H3DAnimVector2D();

                            CopyKeyFrames(((GfxAnimVector2D)Elem.Content).X, Vector.X);
                            CopyKeyFrames(((GfxAnimVector2D)Elem.Content).Y, Vector.Y);

                            Match Path = Regex.Match(Elem.Name, MatCoordScaleREx);

                            bool IsTranslation = !Path.Success;

                            if (IsTranslation)
                            {
                                Path = Regex.Match(Elem.Name, MatCoordTransREx);
                            }

                            if (Path.Success && int.TryParse(Path.Groups[2].Value, out int CoordIdx))
                            {
                                H3DTargetType TargetType = 0;

                                switch (CoordIdx)
                                {
                                    case 0: TargetType = H3DTargetType.MaterialTexCoord0Scale; break;
                                    case 1: TargetType = H3DTargetType.MaterialTexCoord1Scale; break;
                                    case 2: TargetType = H3DTargetType.MaterialTexCoord2Scale; break;
                                }

                                if (TargetType != 0)
                                {
                                    string Name = Path.Groups[1].Value;

                                    if (IsTranslation)
                                    {
                                        TargetType += 2;
                                    }

                                    Output.Elements.Add(new H3DAnimationElement()
                                    {
                                        Name          = Name,
                                        Content       = Vector,
                                        PrimitiveType = H3DPrimitiveType.Vector2D,
                                        TargetType    = TargetType
                                    });
                                }
                            }
                        }
                        break;

                    case GfxPrimitiveType.Vector3D:
                        {
                            H3DAnimVector3D Vector = new H3DAnimVector3D();

                            CopyKeyFrames(((GfxAnimVector3D)Elem.Content).X, Vector.X);
                            CopyKeyFrames(((GfxAnimVector3D)Elem.Content).Y, Vector.Y);
                            CopyKeyFrames(((GfxAnimVector3D)Elem.Content).Z, Vector.Z);

                            H3DTargetType TargetType = 0;

                            switch (Elem.Name)
                            {
                                case ViewUpdaterTarget: TargetType = H3DTargetType.CameraTargetPos;    break;
                                case ViewUpdaterUpVec:  TargetType = H3DTargetType.CameraUpVector;     break;
                                case ViewUpdaterRotate: TargetType = H3DTargetType.CameraViewRotation; break;
                            }

                            if (TargetType != 0)
                            {
                                Output.Elements.Add(new H3DAnimationElement()
                                {
                                    Name          = Elem.Name,
                                    Content       = Vector,
                                    PrimitiveType = H3DPrimitiveType.Vector3D,
                                    TargetType    = TargetType
                                });
                            }
                        }
                        break;

                    case GfxPrimitiveType.Transform:
                        {
                            H3DAnimTransform Transform = new H3DAnimTransform();

                            CopyKeyFrames(((GfxAnimTransform)Elem.Content).ScaleX,       Transform.ScaleX);
                            CopyKeyFrames(((GfxAnimTransform)Elem.Content).ScaleY,       Transform.ScaleY);
                            CopyKeyFrames(((GfxAnimTransform)Elem.Content).ScaleZ,       Transform.ScaleZ);

                            CopyKeyFrames(((GfxAnimTransform)Elem.Content).RotationX,    Transform.RotationX);
                            CopyKeyFrames(((GfxAnimTransform)Elem.Content).RotationY,    Transform.RotationY);
                            CopyKeyFrames(((GfxAnimTransform)Elem.Content).RotationZ,    Transform.RotationZ);

                            CopyKeyFrames(((GfxAnimTransform)Elem.Content).TranslationX, Transform.TranslationX);
                            CopyKeyFrames(((GfxAnimTransform)Elem.Content).TranslationY, Transform.TranslationY);
                            CopyKeyFrames(((GfxAnimTransform)Elem.Content).TranslationZ, Transform.TranslationZ);

                            H3DTargetType TargetType = 0;

                            switch (Output.AnimationType)
                            {
                                case H3DAnimationType.Skeletal: TargetType = H3DTargetType.Bone;            break;
                                case H3DAnimationType.Camera:   TargetType = H3DTargetType.CameraTransform; break;
                                case H3DAnimationType.Light:    TargetType = H3DTargetType.LightTransform;  break;
                            }

                            Output.Elements.Add(new H3DAnimationElement()
                            {
                                Name          = Elem.Name,
                                Content       = Transform,
                                PrimitiveType = H3DPrimitiveType.Transform,
                                TargetType    = TargetType
                            });
                        }
                        break;

                    case GfxPrimitiveType.RGBA:
                        {
                            H3DAnimRGBA RGBA = new H3DAnimRGBA();

                            CopyKeyFrames(((GfxAnimRGBA)Elem.Content).R, RGBA.R);
                            CopyKeyFrames(((GfxAnimRGBA)Elem.Content).G, RGBA.G);
                            CopyKeyFrames(((GfxAnimRGBA)Elem.Content).B, RGBA.B);
                            CopyKeyFrames(((GfxAnimRGBA)Elem.Content).A, RGBA.A);

                            H3DTargetType TargetType = 0;

                            if (Output.AnimationType == H3DAnimationType.Material)
                            {
                                Match Path = Regex.Match(Elem.Name, MatColorREx);

                                if (Path.Success)
                                {
                                    switch (Path.Groups[2].Value)
                                    {
                                        case "Emission": TargetType = H3DTargetType.MaterialEmission; break;
                                        case "Ambient": TargetType = H3DTargetType.MaterialAmbient; break;
                                        case "Diffuse": TargetType = H3DTargetType.MaterialDiffuse; break;
                                        case "Specular0": TargetType = H3DTargetType.MaterialSpecular0; break;
                                        case "Specular1": TargetType = H3DTargetType.MaterialSpecular1; break;
                                        case "Constant0": TargetType = H3DTargetType.MaterialConstant0; break;
                                        case "Constant1": TargetType = H3DTargetType.MaterialConstant1; break;
                                        case "Constant2": TargetType = H3DTargetType.MaterialConstant2; break;
                                        case "Constant3": TargetType = H3DTargetType.MaterialConstant3; break;
                                        case "Constant4": TargetType = H3DTargetType.MaterialConstant4; break;
                                        case "Constant5": TargetType = H3DTargetType.MaterialConstant5; break;
                                    }
                                }
                                else
                                {
                                    Path = Regex.Match(Elem.Name, MatMapperBCREx);

                                    if (Path.Success && int.TryParse(Path.Groups[2].Value, out int MapperIdx))
                                    {
                                        switch (MapperIdx)
                                        {
                                            case 0: TargetType = H3DTargetType.MaterialMapper0BorderCol; break;
                                            case 1: TargetType = H3DTargetType.MaterialMapper1BorderCol; break;
                                            case 2: TargetType = H3DTargetType.MaterialMapper2BorderCol; break;
                                        }
                                    }
                                }

                                if (Path.Success && TargetType != 0)
                                {
                                    string Name = Path.Groups[1].Value;

                                    Output.Elements.Add(new H3DAnimationElement()
                                    {
                                        Name = Name,
                                        Content = RGBA,
                                        PrimitiveType = H3DPrimitiveType.RGBA,
                                        TargetType = TargetType
                                    });
                                }
                            }
                            else if (Output.AnimationType == H3DAnimationType.Light)
                            {
                                switch (Elem.Name)
                                {
                                    case "Ambient": TargetType = H3DTargetType.LightAmbient; break;
                                    case "Diffuse": TargetType = H3DTargetType.LightDiffuse; break;
                                    case "Specular0": TargetType = H3DTargetType.LightSpecular0; break;
                                    case "Specular1": TargetType = H3DTargetType.LightSpecular1; break;
                                    case "GroundColor": TargetType = H3DTargetType.LightSky; break;
                                    case "SkyColor": TargetType = H3DTargetType.LightGround; break;
                                }
                                Output.Elements.Add(new H3DAnimationElement()
                                {
                                    Name = Elem.Name,
                                    Content = RGBA,
                                    PrimitiveType = H3DPrimitiveType.RGBA,
                                    TargetType = TargetType
                                });
                            }
                            else if (Output.AnimationType == H3DAnimationType.Fog)
                            {
                                TargetType = H3DTargetType.FogColor;

                                Output.Elements.Add(new H3DAnimationElement()
                                {
                                    Name = Elem.Name,
                                    Content = RGBA,
                                    PrimitiveType = H3DPrimitiveType.RGBA,
                                    TargetType = TargetType
                                });
                            }
                        }
                        break;

                    case GfxPrimitiveType.QuatTransform:
                        {
                            H3DAnimQuatTransform QuatTransform = new H3DAnimQuatTransform();

                            CopyList(((GfxAnimQuatTransform)Elem.Content).Scales,       QuatTransform.Scales);
                            CopyList(((GfxAnimQuatTransform)Elem.Content).Rotations,    QuatTransform.Rotations);
                            CopyList(((GfxAnimQuatTransform)Elem.Content).Translations, QuatTransform.Translations);

                            Output.Elements.Add(new H3DAnimationElement()
                            {
                                Name          = Elem.Name,
                                Content       = QuatTransform,
                                PrimitiveType = H3DPrimitiveType.QuatTransform,
                                TargetType    = H3DTargetType.Bone
                            });
                        }
                        break;

                    case GfxPrimitiveType.MtxTransform:
                        {
                            H3DAnimMtxTransform MtxTransform = new H3DAnimMtxTransform();

                            GfxAnimMtxTransform Source = (GfxAnimMtxTransform)Elem.Content;

                            MtxTransform.StartFrame = Source.StartFrame;
                            MtxTransform.EndFrame   = Source.EndFrame;

                            MtxTransform.PreRepeat  = (H3DLoopType)Source.PreRepeat;
                            MtxTransform.PostRepeat = (H3DLoopType)Source.PostRepeat;

                            CopyList(Source.Frames, MtxTransform.Frames);

                            Output.Elements.Add(new H3DAnimationElement()
                            {
                                Name          = Elem.Name,
                                Content       = MtxTransform,
                                PrimitiveType = H3DPrimitiveType.MtxTransform,
                                TargetType    = H3DTargetType.Bone
                            });
                        }
                        break;
                    case GfxPrimitiveType.Texture:
                        {
                            H3DAnimFloat Float = new H3DAnimFloat();

                            CopyKeyFrames(((GfxAnimTexture)Elem.Content).Texture, Float.Value);

                            var list = ((GfxAnimTexture)Elem.Content).TextureList.Select(x => x.Path).ToList();
                            ((H3DMaterialAnim)Output).TextureNames = list;

                            H3DTargetType TargetType = H3DTargetType.MaterialMapper0Texture;

                            Match Path = Regex.Match(Elem.Name, MatMapperTexREx);
                            string Name = Elem.Name;

                            if (Path.Success && int.TryParse(Path.Groups[2].Value, out int CoordIdx))
                            {
                                Name = Path.Groups[1].Value;

                                switch (CoordIdx)
                                {
                                    case 0: TargetType = H3DTargetType.MaterialMapper0Texture; break;
                                    case 1: TargetType = H3DTargetType.MaterialMapper1Texture; break;
                                    case 2: TargetType = H3DTargetType.MaterialMapper2Texture; break;
                                }
                            }


                            if (TargetType != 0)
                            {
                                Output.Elements.Add(new H3DAnimationElement()
                                {
                                    Name = Name,
                                    Content = Float,
                                    PrimitiveType = H3DPrimitiveType.Texture,
                                    TargetType = TargetType
                                });
                            }
                        }
                        break;
                }
            }

            return Output;
        }

        public void FromH3D(H3DAnimation animation)
        {
            //Note this code is only for material animations for the time being.
            this.FramesCount = animation.FramesCount;
            this.Elements.Clear();

            switch (animation.AnimationType)
            {
                case H3DAnimationType.Skeletal: this.TargetAnimGroupName = "SkeletalAnimation"; break;
                case H3DAnimationType.Material: this.TargetAnimGroupName = "MaterialAnimation"; break;
                case H3DAnimationType.Visibility: this.TargetAnimGroupName = "VisibilityAnimation"; break;
                case H3DAnimationType.Light: this.TargetAnimGroupName = "LightAnimation"; break;
                case H3DAnimationType.Camera: this.TargetAnimGroupName = "CameraAnimation"; break;
                case H3DAnimationType.Fog: this.TargetAnimGroupName = "FogAnimation"; break;
            }

            foreach (var elem in animation.Elements)
            {
                //skip these types on conversion atm
                switch (elem.PrimitiveType)
                {
                    case H3DPrimitiveType.QuatTransform:
                    case H3DPrimitiveType.MtxTransform:
                        continue;
                }

                string MaterialTarget(string target)
                {
                    return @"Materials[""" + elem.Name + @"""]." + target;
                }

                string MeshNodeVisibilitiesTarget(string target)
                {
                    return @"MeshNodeVisibilities[""" + elem.Name + @"""]." + target;
                }

                string MeshTarget(string target)
                {
                    return @"Meshes[""" + elem.Name + @"""]." + target;
                }

                string CameraTarget(string target)
                {
                    return target;
                }

                GfxAnimationElement gfxElement = new GfxAnimationElement();
                this.Elements.Add(gfxElement);

                switch (elem.TargetType)
                {
                    case H3DTargetType.MaterialMapper0Texture: gfxElement.Name = MaterialTarget("TextureMappers[0].Texture"); break;
                    case H3DTargetType.MaterialMapper1Texture: gfxElement.Name = MaterialTarget("TextureMappers[1].Texture"); break;
                    case H3DTargetType.MaterialMapper2Texture: gfxElement.Name = MaterialTarget("TextureMappers[2].Texture"); break;
                    case H3DTargetType.MaterialTexCoord0Scale: gfxElement.Name = MaterialTarget("TextureCoordinators[0].Scale"); break;
                    case H3DTargetType.MaterialTexCoord1Scale: gfxElement.Name = MaterialTarget("TextureCoordinators[1].Scale"); break;
                    case H3DTargetType.MaterialTexCoord2Scale: gfxElement.Name = MaterialTarget("TextureCoordinators[2].Scale"); break;
                    case H3DTargetType.MaterialTexCoord0Trans: gfxElement.Name = MaterialTarget("TextureCoordinators[0].Translate"); break;
                    case H3DTargetType.MaterialTexCoord1Trans: gfxElement.Name = MaterialTarget("TextureCoordinators[1].Translate"); break;
                    case H3DTargetType.MaterialTexCoord2Trans: gfxElement.Name = MaterialTarget("TextureCoordinators[2].Translate"); break;
                    case H3DTargetType.MaterialTexCoord0Rot: gfxElement.Name = MaterialTarget("TextureCoordinators[0].Rotate"); break;
                    case H3DTargetType.MaterialTexCoord1Rot: gfxElement.Name = MaterialTarget("TextureCoordinators[1].Rotate"); break;
                    case H3DTargetType.MaterialTexCoord2Rot: gfxElement.Name = MaterialTarget("TextureCoordinators[2].Rotate"); break;
                    case H3DTargetType.MaterialDiffuse: gfxElement.Name = MaterialTarget("MaterialColor.Diffuse"); break;
                    case H3DTargetType.MaterialAmbient: gfxElement.Name = MaterialTarget("MaterialColor.Ambient"); break;
                    case H3DTargetType.MaterialEmission: gfxElement.Name = MaterialTarget("MaterialColor.Emission"); break;
                    case H3DTargetType.MaterialSpecular0: gfxElement.Name = MaterialTarget("MaterialColor.Specular0"); break;
                    case H3DTargetType.MaterialSpecular1: gfxElement.Name = MaterialTarget("MaterialColor.Specular1"); break;
                    case H3DTargetType.MaterialConstant0: gfxElement.Name = MaterialTarget("MaterialColor.Constant0"); break;
                    case H3DTargetType.MaterialConstant1: gfxElement.Name = MaterialTarget("MaterialColor.Constant1"); break;
                    case H3DTargetType.MaterialConstant2: gfxElement.Name = MaterialTarget("MaterialColor.Constant2"); break;
                    case H3DTargetType.MaterialConstant3: gfxElement.Name = MaterialTarget("MaterialColor.Constant3"); break;
                    case H3DTargetType.MaterialConstant4: gfxElement.Name = MaterialTarget("MaterialColor.Constant4"); break;
                    case H3DTargetType.MaterialConstant5: gfxElement.Name = MaterialTarget("MaterialColor.Constant5"); break;

                    case H3DTargetType.CameraTransform: gfxElement.Name = "Transform"; break;

                    case H3DTargetType.CameraTargetPos: gfxElement.Name = "ViewUpdater.TargetPosition"; break;
                    case H3DTargetType.CameraUpVector: gfxElement.Name = "ViewUpdater.UpwardVector"; break;
                    case H3DTargetType.CameraViewRotation: gfxElement.Name = "ViewUpdater.ViewRotate"; break;
                    case H3DTargetType.CameraTwist: gfxElement.Name = "ViewUpdater.Twist"; break;

                    case H3DTargetType.CameraZNear: gfxElement.Name = "ProjectionUpdater.Near"; break;
                    case H3DTargetType.CameraZFar: gfxElement.Name = "ProjectionUpdater.Far"; break;
                    case H3DTargetType.CameraAspectRatio: gfxElement.Name = "ProjectionUpdater.AspectRatio"; break;
                    case H3DTargetType.CameraFovY: gfxElement.Name = "ProjectionUpdater.Fovy"; break;
                    case H3DTargetType.CameraHeight: gfxElement.Name = "ProjectionUpdater.Height"; break;

                    case H3DTargetType.MeshNodeVisibility: gfxElement.Name = MeshNodeVisibilitiesTarget("IsVisible"); break;
                    case H3DTargetType.ModelVisibility: gfxElement.Name = "IsVisible"; break;
                    case H3DTargetType.LightEnabled: gfxElement.Name = "IsLightEnabled"; break;
                    case H3DTargetType.LightTransform: gfxElement.Name = "Transform"; break;
                    case H3DTargetType.LightDiffuse: gfxElement.Name = "Diffuse"; break;
                    case H3DTargetType.LightAmbient: gfxElement.Name = "Ambient"; break;
                    case H3DTargetType.LightSpecular0: gfxElement.Name = "Specular0"; break;
                    case H3DTargetType.LightSpecular1: gfxElement.Name = "Specular1"; break;
                    case H3DTargetType.LightDirection: gfxElement.Name = "Direction"; break;
                    case H3DTargetType.LightAttenuationStart: gfxElement.Name = LightDistanceAttenuationStart; break;
                    case H3DTargetType.LightAttenuationEnd: gfxElement.Name = LightDistanceAttenuationEnd; break;
                    case H3DTargetType.LightInterpolationFactor: gfxElement.Name = LightLerpFactor; break;
                    case H3DTargetType.LightGround: gfxElement.Name = "GroundColor"; break;
                    case H3DTargetType.LightSky: gfxElement.Name = "SkyColor"; break;
                    case H3DTargetType.FogColor: gfxElement.Name = "Color"; break;
                    case H3DTargetType.MeshIndexNodeVisibility:  gfxElement.Name = $"{elem.Name}.IsVisible"; break;
                }

                switch (elem.PrimitiveType)
                {
                    case H3DPrimitiveType.Float:
                        {
                            gfxElement.PrimitiveType = GfxPrimitiveType.Float;

                            var animF = new GfxAnimFloat();
                            CopyKeyFrames(((H3DAnimFloat)elem.Content).Value, animF.Value);
                            gfxElement.Content = animF;
                        }
                        break;
                    case H3DPrimitiveType.Integer:
                        {
                            gfxElement.PrimitiveType = GfxPrimitiveType.Integer;

                            var animF = new GfxAnimFloat();
                            CopyKeyFrames(((H3DAnimFloat)elem.Content).Value, animF.Value);
                            gfxElement.Content = animF;
                        }
                        break;
                    case H3DPrimitiveType.Vector2D:
                        {
                            gfxElement.PrimitiveType = GfxPrimitiveType.Vector2D;

                            var animVec = new GfxAnimVector2D();
                            CopyKeyFrames(((H3DAnimVector2D)elem.Content).X, animVec.X);
                            CopyKeyFrames(((H3DAnimVector2D)elem.Content).Y, animVec.Y);
                            gfxElement.Content = animVec;
                        }
                        break;
                    case H3DPrimitiveType.Vector3D:
                        {
                            gfxElement.PrimitiveType = GfxPrimitiveType.Vector3D;

                            var animVec = new GfxAnimVector3D();
                            CopyKeyFrames(((H3DAnimVector3D)elem.Content).X, animVec.X);
                            CopyKeyFrames(((H3DAnimVector3D)elem.Content).Y, animVec.Y);
                            CopyKeyFrames(((H3DAnimVector3D)elem.Content).Z, animVec.Z);
                            gfxElement.Content = animVec;
                        }
                        break;
                    case H3DPrimitiveType.RGBA:
                        {
                            gfxElement.PrimitiveType = GfxPrimitiveType.RGBA;

                            var animVec = new GfxAnimRGBA();
                            CopyKeyFrames(((H3DAnimRGBA)elem.Content).R, animVec.R);
                            CopyKeyFrames(((H3DAnimRGBA)elem.Content).G, animVec.G);
                            CopyKeyFrames(((H3DAnimRGBA)elem.Content).B, animVec.B);
                            CopyKeyFrames(((H3DAnimRGBA)elem.Content).A, animVec.A);
                            gfxElement.Content = animVec;
                        }
                        break;
                    case H3DPrimitiveType.Boolean:
                        {
                            gfxElement.PrimitiveType = GfxPrimitiveType.Boolean;
                            var animB = new GfxAnimBoolean();
                            CopyKeyFrames(((H3DAnimBoolean)elem.Content), animB);
                            gfxElement.Content = animB;
                        }
                        break;
                    case H3DPrimitiveType.Texture:
                        {
                            gfxElement.PrimitiveType = GfxPrimitiveType.Texture;

                            var textures = ((H3DMaterialAnim)animation).TextureNames.ToList();

                            var animTex = new GfxAnimTexture();
                            animTex.TextureList = new Model.Material.GfxTextureReference[textures.Count];
                            for (int i = 0; i < textures.Count; i++)
                                animTex.TextureList[i] = new Model.Material.GfxTextureReference()
                                {
                                    Path = textures[i],
                                    Name = "", //only need to set path
                                };
                            CopyKeyFrames(((H3DAnimFloat)elem.Content).Value, animTex.Texture);
                            gfxElement.Content = animTex;
                        }
                        break;
                    case H3DPrimitiveType.Transform:
                        {
                            gfxElement.PrimitiveType = GfxPrimitiveType.Transform;

                            var transform = new GfxAnimTransform();
                            CopyKeyFrames(((H3DAnimTransform)elem.Content).TranslationX, transform.TranslationX);
                            CopyKeyFrames(((H3DAnimTransform)elem.Content).TranslationY, transform.TranslationY);
                            CopyKeyFrames(((H3DAnimTransform)elem.Content).TranslationZ, transform.TranslationZ);
                            CopyKeyFrames(((H3DAnimTransform)elem.Content).ScaleX, transform.ScaleX);
                            CopyKeyFrames(((H3DAnimTransform)elem.Content).ScaleY, transform.ScaleY);
                            CopyKeyFrames(((H3DAnimTransform)elem.Content).ScaleZ, transform.ScaleZ);
                            CopyKeyFrames(((H3DAnimTransform)elem.Content).RotationX, transform.RotationX);
                            CopyKeyFrames(((H3DAnimTransform)elem.Content).RotationY, transform.RotationY);
                            CopyKeyFrames(((H3DAnimTransform)elem.Content).RotationZ, transform.RotationZ);
                            gfxElement.Content = transform;
                        }
                        break;
                    case H3DPrimitiveType.QuatTransform:
                        {
                            gfxElement.PrimitiveType = GfxPrimitiveType.QuatTransform;

                            var transform = new GfxAnimQuatTransform();
                            var h3dTransform = (H3DAnimQuatTransform)elem.Content;

                            CopyList(h3dTransform.Translations, transform.Translations);
                            CopyList(h3dTransform.Rotations, transform.Rotations);
                            CopyList(h3dTransform.Scales, transform.Scales);

                            gfxElement.Content = transform;
                        }
                        break;
                    case H3DPrimitiveType.MtxTransform:
                        {
                            gfxElement.PrimitiveType = GfxPrimitiveType.MtxTransform;

                            var transform = new GfxAnimMtxTransform();
                            var h3dTransform = (H3DAnimMtxTransform)elem.Content;

                            transform.StartFrame = h3dTransform.StartFrame;
                            transform.EndFrame = h3dTransform.EndFrame;

                            CopyList(h3dTransform.Frames, transform.Frames);

                            gfxElement.Content = transform;
                        }
                        break;
                }
            }
        }

        private void CopyKeyFrames(H3DAnimBoolean Source, GfxAnimBoolean Target)
        {
            Target.StartFrame = Source.StartFrame;
            Target.EndFrame = Source.EndFrame;
           // Target.PreRepeat = (GfxLoopMode)Source.PreRepeat;
           // Target.PostRepeat = (GfxLoopMode)Source.PostRepeat;

            foreach (bool KF in Source.Values)
            {
                Target.Values.Add(KF);
            }
        }

        private void CopyKeyFrames(GfxFloatKeyFrameGroup Source, H3DFloatKeyFrameGroup Target)
        {
            Target.StartFrame = Source.StartFrame;
            Target.EndFrame   = Source.EndFrame;

            Target.PreRepeat  = (H3DLoopType)Source.PreRepeat;
            Target.PostRepeat = (H3DLoopType)Source.PostRepeat;

            Target.Quantization = Source.Quantization;

            if (Source.Quantization == KeyFrameQuantization.StepLinear32 ||
                Source.Quantization == KeyFrameQuantization.StepLinear64)
            {
                Target.InterpolationType = Source.IsLinear
                    ? H3DInterpolationType.Linear
                    : H3DInterpolationType.Step;
            }
            else
            {
                Target.InterpolationType = H3DInterpolationType.Hermite;
            }

            foreach (KeyFrame KF in Source.KeyFrames)
            {
                Target.KeyFrames.Add(KF);
            }
        }

        private void CopyKeyFrames(H3DFloatKeyFrameGroup Source, GfxFloatKeyFrameGroup Target)
        {
            Target.StartFrame = Source.StartFrame;
            Target.EndFrame = Source.EndFrame;

            Target.PreRepeat = (GfxLoopType)Source.PreRepeat;
            Target.PostRepeat = (GfxLoopType)Source.PostRepeat;
            Target.Quantization = Source.Quantization;

            Target.IsLinear = Source.InterpolationType == H3DInterpolationType.Linear;

            foreach (KeyFrame KF in Source.KeyFrames)
            {
                Target.KeyFrames.Add(KF);
            }
        }

        private void CopyList<T>(List<T> Source, List<T> Target)
        {
            foreach (T Item in Source)
            {
                Target.Add(Item);
            }
        }
    }
}
