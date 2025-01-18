using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Configs;
using Sentinel.Plugin.Contracts;
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
        private readonly ILoggerFactory? _loggerFactory;
        private PythonLogger? _pluginLogger;
        private bool disposedValue;

        public PythonPluginLoader()
        {
        }
        public PythonPluginLoader(ILogger<PythonPluginLoader> logger, ILoggerFactory? loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public string Name => "PythonPluginLoader";
        public string Description => "Python plugins loader for sentinel.";

        public CmdOptionsBase CmdOptions { get; set; } = new CmdOptions();
        private ConfigBase _config = new Config();
        public ConfigBase Config
        {
            get => _config;
            set
            {
                _config = value;
                _pluginLogger = new PythonLogger(_loggerFactory?.CreateLogger($"{nameof(PythonLogger)}-{((Config)value).PythonClassName}-{((Config)value).LibraryName}"));
            }
        }
        public void SetConfig(CmdOptionsBase cmdOptions)
        {
            Config.LibraryFolder = cmdOptions.DirectoryPath;
            Config.ChunkSizeBytes = cmdOptions.ChunkSizeBytes;
            Config.ForceCalcDigest = !cmdOptions.DryRun;

            var options = (CmdOptions)cmdOptions;
            var config = (Config)Config;

            config.PythonScriptPath = options.PythonScriptPath;
            config.PythonClassName = options.PythonClassName;

            _pluginLogger = new PythonLogger(_loggerFactory?.CreateLogger($"{nameof(PythonLogger)}-{config.PythonClassName}-{config.LibraryName}"));
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
