using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Configs;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Models;
using Sentinel.Plugin.Options;
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

        public CommandLineOptionsBase CommandLineOptions { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public PluginConfigBase Config { get; set; } = new Config();

        public IEnumerable<AppBinary> GetAppBinaries(CommandLineOptionsBase commandLineOptions)
        {
            throw new NotImplementedException();
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
