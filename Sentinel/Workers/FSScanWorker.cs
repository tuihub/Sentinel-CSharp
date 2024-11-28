using Microsoft.Extensions.Hosting;
using Sentinel.Plugin.Contracts;

namespace Sentinel.Workers
{
    public class FSScanWorker : BackgroundService
    {
        private readonly IPlugin _plugin;

        public FSScanWorker(IPlugin plugin, object config)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _plugin.Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}
