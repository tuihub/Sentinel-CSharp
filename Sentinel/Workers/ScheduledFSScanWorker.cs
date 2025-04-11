using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentinel.Helpers;
using Sentinel.Plugin.Configs;
using Sentinel.Plugin.Contracts;
using Sentinel.Services;

namespace Sentinel.Workers
{
    internal class ScheduledFSScanWorker : BackgroundService
    {
        private readonly ILogger<ScheduledFSScanWorker> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IPlugin _plugin;
        private readonly LibrarianClientService _librarianClientService;
        private readonly TimeSpan _scanInterval;

        private readonly long _appBinaryBaseDirId;

        public ScheduledFSScanWorker(ILogger<ScheduledFSScanWorker> logger, IServiceScopeFactory serviceScopeFactory, IPlugin plugin,
            IConfigurationSection pluginConfigSection, LibrarianClientService librarianClientService, TimeSpan scanInterval)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _plugin = plugin;
            _plugin.Config = pluginConfigSection.Get(plugin.Config.GetType()) as ConfigBase
                ?? throw new Exception($"Failed to parse PluginConfig for {plugin.Name}");
            _librarianClientService = librarianClientService;
            _scanInterval = scanInterval;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SentinelDbContext>();
                _appBinaryBaseDirId = dbContext.AppBinaryBaseDirs
                    .Single(x => x.Path == _plugin.Config.LibraryFolder)
                    .Id;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        using var dbContext = scope.ServiceProvider.GetRequiredService<SentinelDbContext>();
                        var result = await _plugin.DoFullScanAsync(
                            dbContext.AppBinaries
                            .Include(x => x.AppBinaryBaseDir)
                            .Where(x => x.AppBinaryBaseDir.Path == _plugin.Config.LibraryFolder)
                            .Include(x => x.Files)
                            .ThenInclude(x => x.Chunks)
                            .AsNoTracking()
                            .Select(x => x.ToPluginModel()),
                            stoppingToken);
                        stoppingToken.ThrowIfCancellationRequested();

                        await dbContext.ApplyScanChangeResultsAsync(_logger, result, _appBinaryBaseDirId, stoppingToken);

                        await _librarianClientService.ReportAppBinariesAsync(stoppingToken);

                        await Task.Delay(_scanInterval, stoppingToken);
                        stoppingToken.ThrowIfCancellationRequested();
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, $"ScheduledFSScanWorker[{_plugin.Name}, {_plugin.Config.LibraryName}] is stopping.");
            }

            _logger.LogInformation($"ScheduledFSScanWorker[{_plugin.Name}, {_plugin.Config.LibraryName}] has stopped.");
        }
    }
}
