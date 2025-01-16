using CommandLine;
using Sentinel.Plugin.Configs;

namespace Sentinel.Plugin.SubFolder
{
    [Verb("subfolder", aliases: ["sb"], HelpText = "A plugin that handles files in sub folders using policies.")]
    public class CmdOptions : CmdOptionsBase
    {
    }
}
