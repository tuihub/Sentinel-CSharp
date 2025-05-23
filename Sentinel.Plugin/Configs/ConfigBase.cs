﻿namespace Sentinel.Plugin.Configs
{
    public class ConfigBase
    {
        public string LibraryName { get; set; } = string.Empty;
        public string LibraryFolder { get; set; } = null!;
        public long ChunkSizeBytes { get; set; } = 64 * 1024 * 1024;
        public bool ForceCalcDigest { get; set; } = false;
    }
}
