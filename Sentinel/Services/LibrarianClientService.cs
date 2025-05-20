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
    public class LibrarianClientService(ILogger<LibrarianClientService> logger, SystemConfig systemConfig, SentinelConfig sentinelConfig,
        StateService stateService, IServiceScopeFactory serviceScopeFactory)
    {
        private readonly ILogger<LibrarianClientService> _logger = logger;
        private readonly SystemConfig _systemConfig = systemConfig;
        private readonly SentinelConfig _sentinelConfig = sentinelConfig;
        private readonly StateService _stateService = stateService;
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

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
                    .Select(x => x.ToPb(_sentinelConfig.NeedToken))
                    .ToList();
                try
                {
                    if (_systemConfig.MaxPbMsgSizeBytes < 0)
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
                        var maxPbSizeBytes = (int)(_systemConfig.MaxPbMsgSizeBytes * 0.9); // reserve some space
                        var snapshotTime = Timestamp.FromDateTime(DateTime.UtcNow);
                        int totalAppBinaries = sentinelAppBinaries.Count;
                        int currentIndex = 0;
                        int partialAppBinaryFileIndex = 0;
                        ReportAppBinariesResponse? finalResponse = null;

                        while (currentIndex < totalAppBinaries)
                        {
                            var batchAppBinaries = new List<SentinelLibraryAppBinary>();
                            int batchSize = 0;
                            bool isFinalBatch = false;
                            bool partialAppBinaryPending = false;

                            // Process current AppBinary
                            while (currentIndex < totalAppBinaries)
                            {
                                var currentAppBinary = sentinelAppBinaries[currentIndex];

                                if (partialAppBinaryPending)
                                {
                                    AddPartialAppBinaryFilesToBatch(ref partialAppBinaryFileIndex, maxPbSizeBytes, batchAppBinaries, ref batchSize, currentAppBinary);

                                    // Check if all files were processed
                                    if (partialAppBinaryFileIndex >= currentAppBinary.Files.Count)
                                    {
                                        partialAppBinaryFileIndex = 0;
                                        currentIndex++;
                                        partialAppBinaryPending = false;
                                        _logger.LogDebug($"Finished processing AppBinary {currentAppBinary.Name}, added all {currentAppBinary.Files.Count} files");
                                        continue;
                                    }
                                    else
                                    {
                                        partialAppBinaryPending = true;
                                        _logger.LogDebug($"Partially processed AppBinary {currentAppBinary.Name}," +
                                            $" added {partialAppBinaryFileIndex}/{currentAppBinary.Files.Count} files");
                                        break;
                                    }
                                }
                                else
                                {
                                    int fullAppBinarySize = currentAppBinary.CalculateSize();

                                    // Check if we can add the entire AppBinary without exceeding the size limit
                                    if (batchSize + fullAppBinarySize <= maxPbSizeBytes)
                                    {
                                        batchAppBinaries.Add(currentAppBinary);
                                        batchSize += fullAppBinarySize;
                                        currentIndex++;
                                        continue;
                                    }

                                    // Check if current batch size exceeds 3/4 of the maximum allowed size
                                    if (batchSize > maxPbSizeBytes * 0.75 && batchAppBinaries.Count > 0)
                                    {
                                        // Current batch is large enough, send it first
                                        break;
                                    }

                                    AddPartialAppBinaryFilesToBatch(ref partialAppBinaryFileIndex, maxPbSizeBytes, batchAppBinaries, ref batchSize, currentAppBinary);

                                    partialAppBinaryPending = true;
                                    _logger.LogDebug($"Partially processed AppBinary {currentAppBinary.Name}," +
                                        $" added {partialAppBinaryFileIndex}/{currentAppBinary.Files.Count} files");
                                    break;
                                }
                            }

                            isFinalBatch = currentIndex >= totalAppBinaries;

                            if (batchAppBinaries.Count == 0)
                            {
                                var ab = sentinelAppBinaries[currentIndex];
                                throw new Exception($"Single AppBinary [{ab.Name}, {ab.SentinelGeneratedId}] exceeds size limit.");
                            }

                            var request = new ReportAppBinariesRequest
                            {
                                SnapshotTime = snapshotTime,
                                CommitSnapshot = isFinalBatch
                            };
                            request.AppBinaries.AddRange(batchAppBinaries);

                            _logger.LogInformation($"Sending batch of {batchAppBinaries.Count} AppBinaries with {batchAppBinaries.Sum(ab => ab.Files.Count)} files, commit={isFinalBatch}");
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
                        Thread.Sleep(200);
                        Environment.Exit(1);
                    }

                    throw;
                }
                finally
                {
                    _stateService.IsFirstReport = false;
                }
            }

            void AddPartialAppBinaryFilesToBatch(ref int partialAppBinaryFileIndex, int maxPbSizeBytes, List<SentinelLibraryAppBinary> batchAppBinaries,
                ref int batchSize, SentinelLibraryAppBinary currentAppBinary)
            {
                // Create a copy of AppBinary with files
                var appBinaryClone = currentAppBinary.Clone();
                appBinaryClone.Files.Clear();
                batchSize += appBinaryClone.CalculateSize(); // Add base size

                // Add files one by one until size limit is reached
                var files = currentAppBinary.Files;

                while (partialAppBinaryFileIndex < files.Count)
                {
                    var file = files[partialAppBinaryFileIndex];
                    int fileSize = file.CalculateSize();

                    // Check if adding this file would exceed the size limit
                    if (batchSize + fileSize <= maxPbSizeBytes)
                    {
                        appBinaryClone.Files.Add(file);
                        batchSize += fileSize;
                        partialAppBinaryFileIndex++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (appBinaryClone.Files.Count > 0 || currentAppBinary.Files.Count == 0)
                {
                    batchAppBinaries.Add(appBinaryClone);
                }
                else
                {
                    batchSize -= appBinaryClone.CalculateSize();
                }
            }
        }
    }
}
