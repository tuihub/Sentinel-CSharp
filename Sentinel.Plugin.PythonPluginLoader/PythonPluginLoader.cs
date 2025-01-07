using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Configs;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Models;
using Sentinel.Plugin.Options;
using System.Text.Json;

namespace Sentinel.Plugin.PythonPluginLoader
{
    public partial class PythonPluginLoader : IPlugin
    {
        private static readonly JsonSerializerOptions s_jso = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        private readonly ILogger? _logger;

        public PythonPluginLoader()
        {
            InitializePython();
        }
        public PythonPluginLoader(ILogger<PythonPluginLoader> logger)
        {
            _logger = logger;

            InitializePython();
        }

        public string Name => "PythonPluginLoader";
        public string Description => "Python plugins loader for sentinel.";

        public CommandLineOptionsBase CommandLineOptions { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public PluginConfigBase Config { get; set; } = new Config();

        public IEnumerable<AppBinary> GetAppBinaries(CommandLineOptionsBase commandLineOptions)
        {
            throw new NotImplementedException();
        }
    }
}
