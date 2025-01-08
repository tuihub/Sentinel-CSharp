using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Configs;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Models;
using System.Text.Json;

namespace Sentinel.Plugin.PythonPluginLoader
{
    public partial class PythonPluginLoader : IPlugin, IDisposable
    {
        private static readonly JsonSerializerOptions s_jso = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        private readonly ILogger? _logger;
        private bool disposedValue;

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

        public CmdOptionsBase CmdOptions { get; set; } = new CmdOptions();
        public ConfigBase Config { get; set; } = new Config();
        public void SetConfig(CmdOptionsBase cmdOptions)
        {
            Config.LibraryFolder = cmdOptions.DirectoryPath;
            Config.ChunkSizeBytes = cmdOptions.ChunkSizeBytes;
            Config.ForceCalcDigest = !cmdOptions.DryRun;

            var options = (CmdOptions)cmdOptions;
            var config = (Config)Config;

            config.PythonScriptPath = options.PythonScriptPath;
            config.PythonClassName = options.PythonClassName;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                ShutdownPython();

                disposedValue = true;
            }
        }

        ~PythonPluginLoader()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
