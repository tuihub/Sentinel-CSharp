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
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public LibrarianClientService(ILogger<LibrarianClientService> logger, SystemConfig systemConfig, SentinelConfig sentinelConfig,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _systemConfig = systemConfig;
            _sentinelConfig = sentinelConfig;
            _serviceScopeFactory = serviceScopeFactory;
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
                var request = new ReportAppBinariesRequest();
                request.AppBinaries.AddRange(sentinelAppBinaries);
                return await client.ReportAppBinariesAsync(request, cancellationToken: ct);
            }
        }
    }
}
