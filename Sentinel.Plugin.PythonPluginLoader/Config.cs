using Sentinel.Plugin.Configs;

namespace Sentinel.Plugin.PythonPluginLoader
{
    public class Config : ConfigBase
    {
        public string PythonScriptPath { get; set; } = null!;
        public string PythonClassName { get; set; } = "Plugin";
        public Dictionary<string, string> PythonScriptCustomConfig { get; set; } = new Dictionary<string, string>();
    }
}
