using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentinel.Configs;
using Sentinel.Helpers;
using TuiHub.Protos.Librarian.Sephirah.V1;
using static TuiHub.Protos.Librarian.Sephirah.V1.LibrarianSephirahService;

namespace Sentinel.Services
{
    public class LibrarianClientService
    {
        private readonly ILogger<LibrarianClientService> _logger;
        private readonly LibrarianSephirahServiceClient _client;
        private readonly SystemConfig _systemConfig;
        private readonly SentinelConfig _sentinelConfig;
        private readonly SentinelDbContext _dbContext;

        public LibrarianClientService(ILogger<LibrarianClientService> logger, LibrarianSephirahServiceClient librarianSephirahServiceClient,
            SystemConfig systemConfig, SentinelConfig sentinelConfig, SentinelDbContext sentinelDbContext)
        {
            _logger = logger;
            _client = librarianSephirahServiceClient;
            _systemConfig = systemConfig;
            _sentinelConfig = sentinelConfig;
            _dbContext = sentinelDbContext;
        }

        public async Task<ReportSentinelInformationResponse> ReportSentinelInformationAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Reporting Sentinel information");
            var request = new ReportSentinelInformationRequest()
            {
                Scheme = ProtoHelper.ToServerScheme(_sentinelConfig.Scheme),
                GetTokenUrlPath = _sentinelConfig.GetTokenUrlPath,
                DownloadFileUrlPath = _sentinelConfig.DownloadFileUrlPath
            };
            request.Hostnames.AddRange(_sentinelConfig.Hostnames);
            request.Libraries.AddRange(
                _systemConfig.LibraryConfigs
                .Select(x => new ReportSentinelInformationRequest.Types.SentinelLibrary
                {
                    Id = _dbContext.AppBinaryBaseDirs.Single(d => d.Path == x.PluginConfig["LibraryFolder"]!.GetValue<string>()).Id,
                    DownloadBasePath = x.DownloadBasePath
                }));
            return await _client.ReportSentinelInformationAsync(request, cancellationToken: ct);
        }

        public async Task<ReportAppBinariesResponse> ReportAppBinariesAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Reporting AppBinaries");
            var libraryPaths = _systemConfig.LibraryConfigs.Select(x => x.PluginConfig["LibraryFolder"]!.GetValue<string>()).ToList();
            var sentinelAppBinaries = _dbContext.AppBinaries
                .Include(x => x.AppBinaryBaseDir)
                .Where(x => libraryPaths.Contains(x.AppBinaryBaseDir.Path))
                .Include(x => x.Files)
                .ThenInclude(x => x.Chunks)
                .Select(x => new ReportAppBinariesRequest.Types.SentinelAppBinary
                {
                    AppBinary = x.ToProto(_sentinelConfig.NeedToken),
                    SentinelLibraryId = x.AppBinaryBaseDir.Id,
                    SentinelGeneratedId = x.Id.ToString()
                });
            var request = new ReportAppBinariesRequest();
            request.SentinelAppBinaries.AddRange(sentinelAppBinaries);
            return await _client.ReportAppBinariesAsync(request, cancellationToken: ct);
        }
    }
}
