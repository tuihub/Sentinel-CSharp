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
                _logger.LogInformation("Python engine is already initialized.");
            }
            else
            {
                _logger.LogInformation("Initializing Python engine.");
                PythonEngine.Initialize();
            }
        }
    }
}
