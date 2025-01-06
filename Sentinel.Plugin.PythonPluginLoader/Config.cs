using Sentinel.Plugin.Configs;

namespace Sentinel.Plugin.PythonPluginLoader
{
    public class Config : PluginConfigBase
    {
        public string PythonScriptPath { get; set; } = string.Empty;
        public string PythonClassName { get; set; } = string.Empty;
        public Dictionary<string, string> ScriptConfig { get; set; } = new Dictionary<string, string>();
    }
}
