using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentinel.Configs;
using Sentinel.Helpers;
using Sentinel.Plugin.Configs;
using TuiHub.Protos.Librarian.Sephirah.V1;
using static TuiHub.Protos.Librarian.Sephirah.V1.LibrarianSephirahService;

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
                var client = scope.ServiceProvider.GetRequiredService<LibrarianSephirahServiceClient>();
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
                var client = scope.ServiceProvider.GetRequiredService<LibrarianSephirahServiceClient>();
                _logger.LogInformation("Reporting AppBinaries");
                var libraryPaths = _systemConfig.LibraryConfigs.Select(x => x.PluginConfig.Get<ConfigBase>()!.LibraryFolder).ToList();
                var sentinelAppBinaries = dbContext.AppBinaries
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
                return await client.ReportAppBinariesAsync(request, cancellationToken: ct);
            }
        }
    }
}
