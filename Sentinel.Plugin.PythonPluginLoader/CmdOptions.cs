using CommandLine;
using Sentinel.Plugin.Configs;

namespace Sentinel.Plugin.PythonPluginLoader
{
    [Verb("pythonpluginloader", aliases: ["ppl"], HelpText = "A plugin that loads python script.")]
    public class CmdOptions : CmdOptionsBase
    {
        [Option('s', "script-path", Required = true, HelpText = "Python script path to load.")]
        public string PythonScriptPath { get; set; } = null!;
        [Option('c', "script-class", Required = false, Default = "Plugin", HelpText = "Python class to use.")]
        public string PythonClassName { get; set; } = "Plugin";
    }
}
