using Microsoft.Extensions.Logging;

namespace Sentinel.Plugin.PythonPluginLoader
{
    public class PythonLogger
    {
        private readonly ILogger? _logger;

        public PythonLogger(ILogger? logger)
        {
            _logger = logger;
        }

        public void LogDebug(string message)
        {
            _logger?.LogDebug(message);
        }

        public void LogInformation(string message)
        {
            _logger?.LogInformation(message);
        }

        public void LogWarning(string message)
        {
            _logger?.LogWarning(message);
        }

        public void LogError(string message)
        {
            _logger?.LogError(message);
        }

        public void LogCritical(string message)
        {
            _logger?.LogCritical(message);
        }
    }
}
