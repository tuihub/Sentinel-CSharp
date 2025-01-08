using Microsoft.Extensions.Logging;
using Python.Runtime;
using Sentinel.Plugin.Contracts;

namespace Sentinel.Plugin.PythonPluginLoader
{
    public partial class PythonPluginLoader : IPlugin
    {
        private void InitializePython()
        {
            if (PythonEngine.IsInitialized)
            {
                _logger?.LogInformation("Python engine is already initialized.");
            }
            else
            {
                _logger?.LogInformation("Initializing Python engine.");
                PythonEngine.Initialize();
                _logger?.LogInformation("Python engine initialized.");
            }
        }

        private void ShutdownPython()
        {
            if (PythonEngine.IsInitialized)
            {
                _logger?.LogInformation("Shutting down Python engine.");
                var stopWatch = System.Diagnostics.Stopwatch.StartNew();
                PythonEngine.Shutdown();
                stopWatch.Stop();
                _logger?.LogInformation($"Python engine shut down, elapsed time: {stopWatch.Elapsed}.");
            }
            else
            {
                _logger?.LogInformation("Python engine is already shut down.");
            }
        }
    }
}
