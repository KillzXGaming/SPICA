using SPICA.Formats.Common;
using SPICA.PICA.Shader;

namespace SPICA.Formats.CtrH3D.Shader
{
    public class H3DShader : INamed
    {
        public byte[] Program;

        //Those seems to be always null?
#pragma warning disable CS0169 // Le champ 'H3DShader.ShaderAllCommands' n'est jamais utilisé
        private uint[] ShaderAllCommands;
#pragma warning restore CS0169 // Le champ 'H3DShader.ShaderAllCommands' n'est jamais utilisé
#pragma warning disable CS0169 // Le champ 'H3DShader.ShaderProgramCommands' n'est jamais utilisé
        private uint[] ShaderProgramCommands;
#pragma warning restore CS0169 // Le champ 'H3DShader.ShaderProgramCommands' n'est jamais utilisé
#pragma warning disable CS0169 // Le champ 'H3DShader.ShaderSetupCommands' n'est jamais utilisé
        private uint[] ShaderSetupCommands;
#pragma warning restore CS0169 // Le champ 'H3DShader.ShaderSetupCommands' n'est jamais utilisé

        public short VtxShaderIndex;
        public short GeoShaderIndex;

#pragma warning disable CS0169 // Le champ 'H3DShader.BindingAddress' n'est jamais utilisé
        private uint BindingAddress; //SBZ?
#pragma warning restore CS0169 // Le champ 'H3DShader.BindingAddress' n'est jamais utilisé

        private string _Name;

        public string Name
        {
            get => _Name;
            set => _Name = value ?? throw Exceptions.GetNullException("Name");
        }

#pragma warning disable CS0169 // Le champ 'H3DShader.UserDefinedAddress' n'est jamais utilisé
        private uint UserDefinedAddress; //SBZ
#pragma warning restore CS0169 // Le champ 'H3DShader.UserDefinedAddress' n'est jamais utilisé

        public ShaderBinary ToShaderBinary()
        {
            return new ShaderBinary(Program);
        }
    }
}
