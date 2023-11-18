using CommandLine;
using Sentinel.Plugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentinel.Plugin.Zdfx
{
    [Verb("zdfx", aliases: new string[] { "zd" }, HelpText = "A plugin that handles zdfx files.")]
    public class Options : OptionsBase
    {
        [Option("url-prefix", Required = false, HelpText = "Public url prefix.")]
        public string? PublicUrlPrefix { get; set; }
        [Option("depth", Required = true, HelpText = "Folder depth.")]
        public int Depth { get; set; }
        [Option('j', "joiner", Required = false, Default = '-', HelpText = "Joiner for sub-versions.")]
        public char Joiner { get; set; }
    }
}
