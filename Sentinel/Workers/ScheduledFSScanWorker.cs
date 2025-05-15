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
        private readonly StateService _stateService;
        private readonly TimeSpan _scanInterval;

        private readonly long _appBinaryBaseDirId;
        private readonly SemaphoreSlim _reportSemaphore = new(1, 1);
        private bool _hasPendingReport = false;
        private DateTime _lastScanTime = DateTime.MinValue;

        public ScheduledFSScanWorker(
            ILogger<ScheduledFSScanWorker> logger, 
            IServiceScopeFactory serviceScopeFactory, 
            IPlugin plugin,
            IConfigurationSection pluginConfigSection, 
            LibrarianClientService librarianClientService, 
            StateService stateService,
            TimeSpan scanInterval)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _plugin = plugin;
            _plugin.Config = pluginConfigSection.Get(plugin.Config.GetType()) as ConfigBase
                ?? throw new Exception($"Failed to parse PluginConfig for {plugin.Name}");
            _librarianClientService = librarianClientService;
            _stateService = stateService;
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
                // Start a separate task to process the report queue
                _ = ProcessReportTaskAsync(stoppingToken);
                
                while (!stoppingToken.IsCancellationRequested)
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        using var dbContext = scope.ServiceProvider.GetRequiredService<SentinelDbContext>();
                        var result = await _plugin.DoFullScanAsync(
                            dbContext.AppBinaries
                            .Where(x => x.AppBinaryBaseDir.Path == _plugin.Config.LibraryFolder)
                            .Include(x => x.AppBinaryBaseDir)
                            .Include(x => x.Files)
                            .ThenInclude(x => x.Chunks)
                            .AsNoTracking()
                            .Select(x => x.ToPluginModel()),
                            stoppingToken);
                        stoppingToken.ThrowIfCancellationRequested();

                        await dbContext.ApplyScanChangeResultsAsync(_logger, result, _appBinaryBaseDirId, stoppingToken);

                        // Mark that we have a new scan result to report
                        _lastScanTime = DateTime.UtcNow;
                        lock (this)
                        {
                            _hasPendingReport = true;
                        }
                        _logger.LogDebug($"[{_plugin.Name}, {_plugin.Config.LibraryName}] New scan completed at: {_lastScanTime}");

                        await Task.Delay(_scanInterval, stoppingToken);
                        stoppingToken.ThrowIfCancellationRequested();
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, $"[{_plugin.Name}, {_plugin.Config.LibraryName}] is stopping.");
            }

            _logger.LogInformation($"[{_plugin.Name}, {_plugin.Config.LibraryName}] has stopped.");
        }

        private async Task ProcessReportTaskAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    bool hasPending;
                    lock (this)
                    {
                        hasPending = _hasPendingReport;
                    }
                    
                    // Check if there is a pending report
                    if (hasPending)
                    {
                        // Check heartbeat status
                        if (_stateService.IsLastHeartbeatSucceeded)
                        {
                            // Prevent multiple threads from reporting simultaneously
                            if (await _reportSemaphore.WaitAsync(0, stoppingToken))
                            {
                                try
                                {
                                    DateTime reportTime = _lastScanTime;
                                    _logger.LogInformation($"[{_plugin.Name}, {_plugin.Config.LibraryName}] Starting report for scan at: {reportTime}");
                                    
                                    await _librarianClientService.ReportAppBinariesAsync(stoppingToken);
                                    
                                    // Clear the pending flag after successful reporting
                                    lock (this)
                                    {
                                        _hasPendingReport = false;
                                    }
                                    
                                    _logger.LogInformation($"[{_plugin.Name}, {_plugin.Config.LibraryName}] Successfully completed report for scan at: {reportTime}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"[{_plugin.Name}, {_plugin.Config.LibraryName}] Error occurred when reporting app binaries");
                                }
                                finally
                                {
                                    _reportSemaphore.Release();
                                }
                            }
                        }
                        else
                        {
                            _logger.LogDebug($"[{_plugin.Name}, {_plugin.Config.LibraryName}] Waiting for heartbeat to recover before reporting");
                        }
                    }

                    // Wait briefly before checking again
                    await Task.Delay(TimeSpan.FromSeconds(_stateService.SystemConfig.HeartbeatIntervalSeconds), stoppingToken);
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, $"[{_plugin.Name}, {_plugin.Config.LibraryName}] ProcessReportTaskAsync is stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{_plugin.Name}, {_plugin.Config.LibraryName}] Error occurred.");
            }
        }
    }
}
