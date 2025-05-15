using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentinel.Configs;
using Sentinel.Services;

namespace Sentinel.Workers
{
    public class HeartBeatWorker : BackgroundService
    {
        private readonly ILogger<HeartBeatWorker> _logger;
        private readonly SystemConfig _systemConfig;
        private readonly StateService _stateService;
        private readonly LibrarianClientService _librarianClientService;
        private readonly TimeSpan _heartbeatInterval;

        public HeartBeatWorker(
            ILogger<HeartBeatWorker> logger,
            SystemConfig systemConfig,
            StateService stateService,
            LibrarianClientService librarianClientService)
        {
            _logger = logger;
            _systemConfig = systemConfig;
            _stateService = stateService;
            _librarianClientService = librarianClientService;
            _heartbeatInterval = TimeSpan.FromSeconds(_systemConfig.HeartbeatIntervalSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"HeartBeatWorker started with interval {_heartbeatInterval.TotalSeconds:F2} seconds");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await _librarianClientService.HeartbeatAsync(stoppingToken);
                        _stateService.IsLastHeartbeatSucceeded = true;
                        _logger.LogInformation("Heartbeat succeeded");
                    }
                    catch (Exception ex)
                    {
                        _stateService.IsLastHeartbeatSucceeded = false;
                        _logger.LogError(ex, "Heartbeat failed");
                    }

                    await Task.Delay(_heartbeatInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "HeartBeatWorker is stopping");
            }

            _logger.LogInformation("HeartBeatWorker has stopped");
        }
    }
}