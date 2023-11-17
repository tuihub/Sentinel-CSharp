using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentinel.Plugin.Models
{
    public class OptionsBase
    {
        [Option('u', "url", Required = true, HelpText = "Librarian host url.", SetName = "server")]
        public string? LibrarianUrl { get; set; }
        [Option('t', "token", Required = true, HelpText = "Librarian host token.", SetName = "server")]
        public string? LibrarianToken { get; set; }
        [Option('c', "console", Required = true, HelpText = "Print to console.", SetName = "console")]
        public bool PrintToConsole { get; set; }
        [Option('d', "dir", Required = true, HelpText = "The directory to scan.")]
        public string DirectoryPath { get; set; } = string.Empty;
    }
}
