﻿using Microsoft.Extensions.Configuration;

namespace Sentinel.Configs
{
    public class LibraryConfig
    {
        public string PluginName { get; set; } = string.Empty;
        public string DownloadBasePath { get; set; } = string.Empty;
        public IConfigurationSection PluginConfig { get; set; } = null!;
    }
}
