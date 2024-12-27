﻿using CommandLine;

namespace Sentinel.Plugin.Options
{
    public class CommandLineOptionsBase
    {
        [Option('c', "console", Required = true, HelpText = "Print to console.", SetName = "console")]
        public bool PrintToConsole { get; set; }

        [Option('d', "dir", Required = true, HelpText = "The directory to scan.")]
        public string DirectoryPath { get; set; } = string.Empty;
        [Option('n', "dry-run", Required = false, Default = false, HelpText = "Dry run, not calculating SHA256 checksum.")]
        public bool DryRun { get; set; }
        [Option("chunk-size", Required = false, Default = 1024 * 1024 * 64, HelpText = "Chunk size in bytes. Default is 64 MiB.")]
        public long ChunkSizeBytes { get; set; }
    }
}