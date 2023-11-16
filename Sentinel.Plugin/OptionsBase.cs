using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentinel.Plugin
{
    class OptionsBase
    {
        [CommandLine.Option('d', "dir", Required = true, HelpText = "The directory to search.")]
        public string DirPath { get; set; } = null!;
    }
}
