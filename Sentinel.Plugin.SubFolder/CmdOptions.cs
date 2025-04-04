using CommandLine;
using Sentinel.Plugin.Configs;

namespace Sentinel.Plugin.SubFolder
{
    [Verb("subfolder", aliases: ["subf"], HelpText = "A plugin that handles files in sub folders using policies.")]
    public class CmdOptions : CmdOptionsBase
    {
        [Option("min-depth", Required = false, Default = 1, HelpText = "Scan min depth.")]
        public int MinDepth { get; set; } = 1;
        [Option("scan-policy", Required = false, Default = ScanPolicy.UntilAnyFile, HelpText = "Scan policy.")]
        public ScanPolicy ScanPolicy { get; set; } = ScanPolicy.UntilAnyFile;
    }
}
