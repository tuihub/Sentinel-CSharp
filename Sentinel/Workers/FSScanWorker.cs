using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;

namespace Sentinel.Workers
{
    public class FSScanWorker
    {
        private readonly ILogger<FSWatchWorker> _logger;
        private readonly SentinelDbContext _dbContext;
        private IPlugin _plugin = null!;

        public FSScanWorker(ILogger<FSWatchWorker> logger, SentinelDbContext dbContext, IPlugin plugin)
        {
            _logger = logger;
            _dbContext = dbContext;

            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        public async void ExecuteAsync(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}
