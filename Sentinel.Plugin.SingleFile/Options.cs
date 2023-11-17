using CommandLine;
using Sentinel.Plugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentinel.Plugin.SingleFile
{
    [Verb("singlefile", aliases: new string[] { "sf" }, HelpText = "A plugin that handles single files.")]
    public class Options : OptionsBase
    {
        [Option("url-prefix", Required = false, HelpText = "Public url prefix.")]
        public string? PublicUrlPrefix { get; set; }
    }
}
