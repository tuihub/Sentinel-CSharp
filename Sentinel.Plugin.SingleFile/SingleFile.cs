﻿using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Configs;
using Sentinel.Plugin.Contracts;

namespace Sentinel.Plugin.SingleFile
{
    public partial class SingleFile : IPlugin
    {
        private readonly ILogger? _logger;
        public SingleFile() { }
        public SingleFile(ILogger<SingleFile> logger)
        {
            _logger = logger;
        }

        public string Name => "SingleFile";
        public string Description => "A sentinel plugin that handles single files.";
        public CmdOptionsBase CmdOptions { get; set; } = new CmdOptions();
        public ConfigBase Config { get; set; } = new Config();
    }
}
