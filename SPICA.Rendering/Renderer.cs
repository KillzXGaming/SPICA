using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Light;
using SPICA.Formats.CtrH3D.LUT;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Shader;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Rendering.Properties;
using SPICA.Rendering.Shaders;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace SPICA.Rendering
{
    public class Renderer : IDisposable
    {
        public int DebugShadingMode = 0;

        public static int SelectedBoneID = -1;
        public static int DebugLUTShadingMode = 0;

        //Global uniforms for when no hemi lighting source is present
        public static Vector3 GlobalHsLGCol = new Vector3(0.4f, 0.4f, 0.4f);
        public static Vector3 GlobalHsLSCol = new Vector3(0.9f, 0.9f, 0.9f);

        public static Dictionary<string, Texture> TextureCache = new Dictionary<string, Texture>();
        public static Dictionary<string, LUT> LUTCache = new Dictionary<string, LUT>();

        public readonly List<Model> Models;
        public readonly List<Light> Lights;

        public readonly Dictionary<string, Texture> Textures;
        public readonly Dictionary<string, LUT> LUTs;
        public readonly Dictionary<string, VertexShader> Shaders;

        public Color4 SceneAmbient;

        private VertexShader DefaultShader;

        public readonly Camera Camera;

        internal int Width, Height;

        private Texture DefaultTexture;
        private Texture UVTestTexture;
        private Texture WeightRampTexture1;
        private Texture WeightRampTexture2;

        private LUT DefaultLUT;

        public Renderer(int Width, int Height)
        {
            Models = new List<Model>();
            Lights = new List<Light>();

            Textures = new Dictionary<string, Texture>();
            LUTs = new Dictionary<string, LUT>();
            Shaders = new Dictionary<string, VertexShader>();

            if (System.IO.File.Exists("Default.png"))
                DefaultTexture = new Texture(new H3DTexture("Default.png"));

            UVTestTexture = new Texture(new H3DTexture("UVPattern", Resources.UVPattern));
            WeightRampTexture1 = new Texture(new H3DTexture("WeightRampTexture1", Resources.boneWeightGradient));
            WeightRampTexture2 = new Texture(new H3DTexture("WeightRampTexture2", Resources.boneWeightGradient2));

            var lut = new H3DLUT() { Name = "Default" };
            lut.Samplers.Add(new H3DLUTSampler() { Name = "Default" });
            lut.Samplers.Add(new H3DLUTSampler() { Name = "DefaultBlack" });
            lut.Samplers[0].CreateLerp(0, 0.0f, 128, 1.0f);
            lut.Samplers[1].CreateLerp(0, 0.0f, 128, 0.0f);

            DefaultLUT = new LUT(lut);

            int VertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);

            GL.ShaderSource(VertexShaderHandle, Resources.DefaultVertexShader);
            GL.CompileShader(VertexShaderHandle);

            Shader.CheckCompilation(VertexShaderHandle);

            DefaultShader = new VertexShader(VertexShaderHandle);

            Camera = new Camera(this);

            Resize(Width, Height);

            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
        }

        /// <summary>
        /// Adds cached instances of an existing render to draw as
        /// </summary>
        public void LoadCached(Renderer render)
        {
            foreach (var model in render.Models)
                Models.Add(new Model(this, model));
            foreach (var tex in render.Textures)
                Textures.Add(tex.Key, tex.Value);
            foreach (var lut in render.LUTs)
                LUTs.Add(lut.Key, lut.Value);
            foreach (var shr in render.Shaders)
                Shaders.Add(shr.Key, shr.Value);
        }

        public void Resize(int Width, int Height)
        {
            this.Width = Width;
            this.Height = Height;

            GL.Viewport(0, 0, Width, Height);

            Camera.RecalculateMatrices();
        }

        public void SetBackgroundColor(Color Color)
        {
            GL.ClearColor(Color);
        }

        public void Merge(H3D Scene)
        {
            Merge(Scene.Models);
            Merge(Scene.Textures);
            Merge(Scene.LUTs);
            Merge(Scene.Lights);
            Merge(Scene.Shaders);
        }

        public void Delete(Renderer render)
        {
            foreach (var tex in render.Textures)
            {
                if (TextureCache.ContainsKey(tex.Key))
                    TextureCache.Remove(tex.Key);
            }
            foreach (var lut in render.LUTs)
            {
                if (LUTCache.ContainsKey(lut.Key))
                    LUTCache.Remove(lut.Key);
            }

            foreach (var Model in render.Models)
                this.Models.Remove(Model);
            foreach (var tex in render.Textures)
                this.Textures.Remove(tex.Key);
            foreach (var lut in render.LUTs)
                this.LUTs.Remove(lut.Key);
            foreach (var light in render.Lights)
                this.Lights.Remove(light);
            foreach (var shader in render.Shaders)
                this.Shaders.Remove(shader.Key);

            render.DeleteAll();
        }

        public void Merge(Renderer render)
        {
            foreach (var Model in render.Models)
                this.Models.Add(Model);
            foreach (var tex in render.Textures)
                this.Textures.Add(tex.Key, tex.Value);
            foreach (var lut in render.LUTs)
                this.LUTs.Add(lut.Key, lut.Value);
            foreach (var light in render.Lights)
                this.Lights.Add(light);
            foreach (var shader in render.Shaders)
                this.Shaders.Add(shader.Key, shader.Value);
        }

        public void Merge(H3DDict<H3DModel> Models)
        {
            foreach (H3DModel Model in Models)
            {
                this.Models.Add(new Model(this, Model));
            }
        }

        public void Merge(H3DDict<H3DTexture> Textures)
        {
            foreach (H3DTexture Texture in Textures)
            {
                this.Textures.Add(Texture.Name, new Texture(Texture));
            }
        }

        public void Merge(H3DDict<H3DLUT> LUTs)
        {
            foreach (H3DLUT LUT in LUTs)
            {
                this.LUTs.Add(LUT.Name, new LUT(LUT));
            }
        }

        public void Merge(H3DDict<H3DLight> Lights)
        {
            foreach (H3DLight Light in Lights)
            {
                this.Lights.Add(new Light(Light));
            }
        }

        public void Merge(H3DDict<H3DShader> Shaders)
        {
            if (Shaders.Count > 0)
            {
                foreach (H3DShader Shader in Shaders)
                {
                    this.Shaders.Add(Shader.Name, new VertexShader(Shader));
                }

                UpdateAllShaders();
            }
        }

        public void DeleteAll()
        {
            DisposeAndClear(Models);
            DisposeAndClear(Textures);
            DisposeAndClear(LUTs);
            DisposeAndClear(Shaders);

            Lights.Clear();
        }

        public void Clear()
        {
            GL.DepthMask(true);
            GL.ColorMask(true, true, true, true);
            GL.Clear(
                ClearBufferMask.ColorBufferBit |
                ClearBufferMask.StencilBufferBit |
                ClearBufferMask.DepthBufferBit);
        }

        public void Render(bool isRenderSelected = false)
        {
            foreach (Model Model in Models)
                Model.RenderLayer1(isRenderSelected);
            foreach (Model Model in Models)
                Model.RenderLayer2(isRenderSelected);
            foreach (Model Model in Models)
                Model.RenderLayer3(isRenderSelected);
            foreach (Model Model in Models)
                Model.RenderLayer4(isRenderSelected);
        }


        public void UpdateAllShaders()
        {
            foreach (Model Model in Models)
            {
                Model.UpdateShaders();
            }
        }

        public void UpdateAllUniforms()
        {
            foreach (Model Model in Models)
            {
                Model.UpdateUniforms();
            }
        }

        internal bool BindUVTestPattern(int Unit)
        {
            UVTestTexture.Bind(Unit);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            return true;
        }

        internal bool BindWeightRamp1(int Unit)
        {
            WeightRampTexture1.Bind(Unit);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            return true;
        }

        internal bool BindWeightRamp2(int Unit)
        {
            WeightRampTexture2.Bind(Unit);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            return true;
        }

        internal bool TryBindTexture(int Unit, string TextureName)
        {
            if (TextureName != null && Textures.TryGetValue(TextureName, out Texture Texture))
            {
                Texture.Bind(Unit);

                return true;
            }
            else if (TextureName != null && TextureCache.TryGetValue(TextureName, out Texture tex))
            {
                tex.Bind(Unit);
                return true;
            }
            else if (DefaultTexture != null)
            {
                DefaultTexture.Bind(Unit);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

                return false;
            }

            return false;
        }

        internal bool TryBindLUT(int Unit, string TableName, string SamplerName)
        {
            if (TableName != null && LUTs.TryGetValue(TableName, out LUT LUT))
            {
                if (!LUT.BindSampler(Unit, SamplerName))
                    return DefaultLUT.BindSampler(Unit, "Default");
                else
                    return true;
            }
            else if (TableName != null && LUTCache.TryGetValue(TableName, out LUT lut))
            {
                return lut.BindSampler(Unit, SamplerName);
            }
            else
            {
                return DefaultLUT.BindSampler(Unit, "DefaultBlack");
            }

            return false;
        }

        internal VertexShader GetShader(string ShaderName)
        {
            if (ShaderName == null || !Shaders.TryGetValue(ShaderName, out VertexShader Output))
            {
                Output = DefaultShader;
            }

            return Output;
        }

        private void DisposeAndClear<T>(List<T> Values) where T : IDisposable
        {
            foreach (T Value in Values)
            {
                Value.Dispose();
            }

            Values.Clear();
        }

        private void DisposeAndClear<T>(Dictionary<string, T> Dict) where T : IDisposable
        {
            foreach (T Value in Dict.Values)
            {
                Value.Dispose();
            }

            Dict.Clear();
        }

        private bool Disposed;

        protected virtual void Dispose(bool Disposing)
        {
            if (!Disposed)
            {
                DeleteAll();

                DefaultShader.Dispose();

                Disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
