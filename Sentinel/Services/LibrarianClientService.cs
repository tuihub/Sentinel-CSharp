using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentinel.Configs;
using Sentinel.Plugin.Configs;
using TuiHub.Protos.Librarian.Sephirah.V1.Sentinel;
using static TuiHub.Protos.Librarian.Sephirah.V1.Sentinel.LibrarianSentinelService;

namespace Sentinel.Services
{
    public class LibrarianClientService
    {
        private readonly ILogger<LibrarianClientService> _logger;
        private readonly SystemConfig _systemConfig;
        private readonly SentinelConfig _sentinelConfig;
        private readonly StateService _stateService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public LibrarianClientService(ILogger<LibrarianClientService> logger, SystemConfig systemConfig, SentinelConfig sentinelConfig,
            StateService stateService, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _systemConfig = systemConfig;
            _sentinelConfig = sentinelConfig;
            _stateService = stateService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<HeartbeatResponse> HeartbeatAsync(CancellationToken ct = default)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var client = scope.ServiceProvider.GetRequiredService<LibrarianSentinelServiceClient>();
                _logger.LogInformation("Sending heartbeat");
                var request = new HeartbeatRequest()
                {
                    InstanceId = _stateService.InstanceId,
                    ClientTime = Timestamp.FromDateTime(DateTime.UtcNow),
                    HeartbeatInterval = Duration.FromTimeSpan(TimeSpan.FromSeconds(_systemConfig.HeartbeatIntervalSeconds)),
                    CommitSnapshotInterval = Duration.FromTimeSpan(TimeSpan.FromMinutes(_systemConfig.LibraryScanIntervalMinutes)),
                };
                return await client.HeartbeatAsync(request, cancellationToken: ct);
            }
        }

        public async Task<ReportSentinelInformationResponse> ReportSentinelInformationAsync(CancellationToken ct = default)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                using var dbContext = scope.ServiceProvider.GetRequiredService<SentinelDbContext>();
                var client = scope.ServiceProvider.GetRequiredService<LibrarianSentinelServiceClient>();
                _logger.LogInformation("Reporting Sentinel information");
                var request = new ReportSentinelInformationRequest()
                {
                    Url = _sentinelConfig.Urls.FirstOrDefault(),
                    GetTokenPath = _sentinelConfig.GetTokenUrlPath,
                    DownloadFileBasePath = _sentinelConfig.DownloadFileUrlPath
                };
                request.UrlAlternatives.AddRange(_sentinelConfig.Urls.Skip(1));
                request.Libraries.AddRange(
                    _systemConfig.LibraryConfigs
                    .Select(x => new SentinelLibrary
                    {
                        Id = dbContext.AppBinaryBaseDirs.Single(d => d.Path == x.PluginConfig.Get<ConfigBase>()!.LibraryFolder).Id,
                        DownloadBasePath = x.DownloadBasePath
                    }));
                return await client.ReportSentinelInformationAsync(request, cancellationToken: ct);
            }
        }

        public async Task<ReportAppBinariesResponse> ReportAppBinariesAsync(CancellationToken ct = default)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                using var dbContext = scope.ServiceProvider.GetRequiredService<SentinelDbContext>();
                var client = scope.ServiceProvider.GetRequiredService<LibrarianSentinelServiceClient>();
                _logger.LogInformation("Reporting AppBinaries");
                var libraryPaths = _systemConfig.LibraryConfigs.Select(x => x.PluginConfig.Get<ConfigBase>()!.LibraryFolder).ToList();
                var sentinelAppBinaries = dbContext.AppBinaries
                    .Where(x => libraryPaths.Contains(x.AppBinaryBaseDir.Path))
                    .Include(x => x.AppBinaryBaseDir)
                    .Include(x => x.Files)
                    .ThenInclude(x => x.Chunks)
                    .AsNoTracking()
                    .Select(x => x.ToPB(_sentinelConfig.NeedToken));
                try
                {
                    if (_systemConfig.MaxPBMsgSizeBytes < 0)
                    {
                        var request = new ReportAppBinariesRequest()
                        {
                            SnapshotTime = Timestamp.FromDateTime(DateTime.UtcNow),
                            CommitSnapshot = true
                        };
                        request.AppBinaries.AddRange(sentinelAppBinaries);
                        return await client.ReportAppBinariesAsync(request, cancellationToken: ct);
                    }
                    // split into multiple messages if the size exceeds the limit
                    else
                    {
                        var maxPBSizeBytes = (int)(_systemConfig.MaxPBMsgSizeBytes * 0.9); // reserve some space
                        var snapshotTime = Timestamp.FromDateTime(DateTime.UtcNow);
                        var appBinariesList = sentinelAppBinaries.ToList();
                        int totalAppBinaries = appBinariesList.Count;
                        int currentIndex = 0;
                        ReportAppBinariesResponse? finalResponse = null;

                        while (currentIndex < totalAppBinaries)
                        {
                            var batch = new List<SentinelLibraryAppBinary>();
                            int batchSize = 0;
                            bool isFinalBatch = false;

                            while (currentIndex < totalAppBinaries)
                            {
                                var appBinary = appBinariesList[currentIndex];
                                int appBinarySize = appBinary.CalculateSize();

                                if (batchSize + appBinarySize <= maxPBSizeBytes || batch.Count == 0)
                                {
                                    batch.Add(appBinary);
                                    batchSize += appBinarySize;
                                    currentIndex++;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            isFinalBatch = currentIndex >= totalAppBinaries;

                            var request = new ReportAppBinariesRequest
                            {
                                SnapshotTime = snapshotTime,
                                CommitSnapshot = isFinalBatch
                            };
                            request.AppBinaries.AddRange(batch);

                            _logger.LogInformation($"Sending batch of {batch.Count} AppBinaries, commit={isFinalBatch}");
                            var response = await client.ReportAppBinariesAsync(request, cancellationToken: ct);

                            if (isFinalBatch)
                            {
                                finalResponse = response;
                            }
                        }

                        return finalResponse ?? new ReportAppBinariesResponse();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to report app binaries");

                    if (_stateService.IsFirstReport && _systemConfig.ExitOnFirstReportFailure)
                    {
                        _logger.LogCritical("First report failed, exiting application...");
                        Environment.Exit(1);
                    }

                    return new ReportAppBinariesResponse();
                }
                finally
                {
                    _stateService.IsFirstReport = false;
                }
            }
        }
    }
}
