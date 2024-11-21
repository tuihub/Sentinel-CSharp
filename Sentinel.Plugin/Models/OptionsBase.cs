using CommandLine;

namespace Sentinel.Plugin.Models
{
    public class OptionsBase
    {
        [Option('d', "dir", Required = true, HelpText = "The directory to scan.")]
        public string DirectoryPath { get; set; } = string.Empty;
        [Option('n', "dry-run", Required = false, HelpText = "Dry run, not calculating SHA256 checksum.")]
        public bool DryRun { get; set; }
    }
}
