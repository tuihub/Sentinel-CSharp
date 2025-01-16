using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Configs;
using Sentinel.Plugin.Contracts;

namespace Sentinel.Plugin.SubFolder
{
    public partial class SubFolder : IPlugin
    {
        private readonly ILogger? _logger;
        public SubFolder() { }
        public SubFolder(ILogger<SubFolder> logger)
        {
            _logger = logger;
        }

        public string Name => "SubFolder";
        public string Description => "A sentinel plugin that handles files in sub folders using policies.";
        public CmdOptionsBase CmdOptions { get; set; } = new CmdOptions();
        public ConfigBase Config { get; set; } = new Config();
    }
}
