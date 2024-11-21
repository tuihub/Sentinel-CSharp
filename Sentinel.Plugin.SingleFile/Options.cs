using CommandLine;
using Sentinel.Plugin.Models;

namespace Sentinel.Plugin.SingleFile
{
    [Verb("singlefile", aliases: ["sf"], HelpText = "A plugin that handles single files.")]
    public class Options : OptionsBase
    {
        [Option("url-prefix", Required = false, HelpText = "Public url prefix.")]
        public string? PublicUrlPrefix { get; set; }
    }
}
