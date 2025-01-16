using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Results;

namespace Sentinel.Helpers
{
    public static class ScanChangeResultHelper
    {
        public static async Task ApplyScanChangeResultsAsync(this SentinelDbContext dbContext, ILogger? logger, ScanChangeResult result,
            long appBinaryBaseDirId, CancellationToken ct = default)
        {
            foreach (var appBinary in result.AppBinariesToRemove)
            {
                ct.ThrowIfCancellationRequested();
                var dbAppBinary = await dbContext.AppBinaries
                    .Include(x => x.Files)
                    .ThenInclude(x => x.Chunks)
                    .SingleOrDefaultAsync(x => x.Path == appBinary.Path, ct);
                if (dbAppBinary != null)
                {
                    dbContext.AppBinaries.Remove(dbAppBinary);
                    logger?.LogDebug("ApplyScanChangeResultsAsync: removing {appBinary} -> AppBinary removed.", appBinary);
                }
                else
                {
                    logger?.LogWarning("ApplyScanChangeResultsAsync: removing {appBinary} -> AppBinary not found.", appBinary);
                }
            }

            foreach (var appBinary in result.AppBinariesToUpdate)
            {
                ct.ThrowIfCancellationRequested();
                var dbAppBinary = await dbContext.AppBinaries
                    .Include(x => x.Files)
                    .ThenInclude(x => x.Chunks)
                    .SingleOrDefaultAsync(x => x.Path == appBinary.Path, ct);
                if (dbAppBinary != null)
                {
                    dbAppBinary.SizeBytes = appBinary.SizeBytes;
                    dbAppBinary.Files = appBinary.Files.Select(x => new Models.Db.AppBinaryFile(x)).ToList();
                    logger?.LogDebug("ApplyScanChangeResultsAsync: updating {appBinary} -> AppBinary updated.", appBinary);
                }
                else
                {
                    logger?.LogWarning("ApplyScanChangeResultsAsync: updating {appBinary} -> AppBinary not found.", appBinary);
                }
            }

            foreach (var appBinary in result.AppBinariesToAdd)
            {
                ct.ThrowIfCancellationRequested();
                var dbAppBinary = await dbContext.AppBinaries.SingleOrDefaultAsync(x => x.Path == appBinary.Path, ct);
                if (dbAppBinary == null)
                {
                    dbContext.AppBinaries.Add(new Models.Db.AppBinary(appBinary, appBinaryBaseDirId));
                    logger?.LogDebug("ApplyScanChangeResultsAsync: adding {appBinary} -> AppBinary added.", appBinary);
                }
                else
                {
                    logger?.LogWarning("ApplyScanChangeResultsAsync: adding {appBinary} -> AppBinary already exists.", appBinary);
                }
            }

            await dbContext.SaveChangesAsync(ct);
        }
    }
}
