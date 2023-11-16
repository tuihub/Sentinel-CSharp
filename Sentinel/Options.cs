using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentinel
{
    public class Options
    {
        [Option('u', "url", HelpText = "Librarian host url.", SetName = "server")]
        public string? LibrarianUrl { get; set; }
        [Option('c', "console", HelpText = "Print to console.", SetName = "console")]
        public bool PrintToConsole { get; set; }
        [Option('d', "dir", Required = true, HelpText = "The directory to scan.")]
        public string DirectoryPath { get; set; } = string.Empty;
    }
}
