using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentinel.Helpers;
using Sentinel.Plugin.Contracts;

namespace Sentinel.Workers
{
    internal class ScheduledFSScanWorker : BackgroundService
    {
        private readonly ILogger<ScheduledFSScanWorker> _logger;
        private readonly SentinelDbContext _dbContext;
        private readonly IPlugin _plugin;
        private readonly TimeSpan _scanInterval;

        private long _appBinaryBaseDirId;

        public ScheduledFSScanWorker(ILogger<ScheduledFSScanWorker> logger, SentinelDbContext dbContext, IPlugin plugin, TimeSpan scanInterval)
        {
            _logger = logger;
            _dbContext = dbContext;
            _plugin = plugin;
            _scanInterval = scanInterval;

            _appBinaryBaseDirId = _dbContext.AppBinaryBaseDirs
                .Single(x => x.Path == _plugin.Config.LibraryFolder)
                .Id;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (true)
                {
                    var result = await _plugin.DoFullScanAsync(
                        _dbContext.AppBinaries
                        .Include(x => x.AppBinaryBaseDir)
                        .Where(x => x.AppBinaryBaseDir.Path == _plugin.Config.LibraryFolder)
                        .Include(x => x.Files)
                        .ThenInclude(x => x.Chunks)
                        .Select(x => x.ToPluginModel()),
                        stoppingToken);
                    stoppingToken.ThrowIfCancellationRequested();

                    await _dbContext.ApplyScanChangeResultsAsync(_logger, result, _appBinaryBaseDirId, stoppingToken);

                    await Task.Delay(_scanInterval, stoppingToken);
                    stoppingToken.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "ScheduledFSScanWorker is stopping.");
            }
        }
    }
}
