using CommandLine;
using Sentinel.Plugin.Configs;

namespace Sentinel.Plugin.SingleFile
{
    [Verb("singlefile", aliases: ["sf"], HelpText = "A plugin that handles single files.")]
    public class CmdOptions : CmdOptionsBase
    {
    }
}
