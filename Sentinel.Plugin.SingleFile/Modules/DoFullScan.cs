using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Helpers;
using Sentinel.Plugin.Models;
using Sentinel.Plugin.Results;

namespace Sentinel.Plugin.SingleFile
{
    public partial class SingleFile : IPlugin
    {
        public async Task<ScanChangeResult> DoFullScanAsync(IQueryable<AppBinary> appBinaries, CancellationToken ct = default)
        {
            var fsFiles = Directory.EnumerateFiles(Config.LibraryFolder, "*", SearchOption.AllDirectories);
            var filesToRemove = appBinaries.Select(x => x.Files.First().FilePath).Except(fsFiles);
            var filesToAdd = fsFiles.Except(appBinaries.Select(x => x.Files.First().FilePath));
            var appBinariesToRecheck = appBinaries.ExceptBy(filesToRemove, x => x.Path);

            _logger?.LogDebug($"DoFullScanAsync: Files to remove: {string.Join(", ", filesToRemove)}");

            var appBinariesToAdd = new List<AppBinary>(filesToAdd.Count());
            foreach (var file in filesToAdd)
            {
                _logger?.LogInformation($"DoFullScanAsync: Adding {file}");
                var fileEntry = await FileEntryHelper.GetFileEntryAsync(_logger, file, file, Config.ChunkSizeBytes, ct: ct);
                appBinariesToAdd.Add(new AppBinary(file, fileEntry.SizeBytes, [fileEntry]));
            }

            var appBinariesToUpdate = new List<AppBinary>();
            foreach (var appBinary in appBinariesToRecheck)
            {
                if (Config.ForceCalcDigest || File.GetLastWriteTimeUtc(appBinary.Path) != appBinary.Files.First().LastWriteUtc)
                {
                    _logger?.LogInformation("DoFullScanAsync: Rechecking " + appBinary.Path);
                    var fileEntry = await FileEntryHelper.GetFileEntryAsync(_logger, appBinary.Path, appBinary.Path, Config.ChunkSizeBytes, ct: ct);
                    if (fileEntry.Sha256 != appBinary.Files.First().Sha256)
                    {
                        appBinariesToUpdate.Add(new AppBinary(appBinary.Path, fileEntry.SizeBytes, [fileEntry]));
                    }
                }
                else
                {
                    _logger?.LogInformation("DoFullScanAsync: Skipping " + appBinary.Path + " because its LastWriteUtc is not changed.");
                }
            }

            return new ScanChangeResult
            {
                AppBinaryPathsToRemove = filesToRemove,
                AppBinariesToAdd = appBinariesToAdd,
                AppBinariesToUpdate = appBinariesToUpdate
            };
        }

        public ScanChangeResult DoFullScan(IQueryable<AppBinary> appBinaries)
        {
            return DoFullScanAsync(appBinaries).Result;
        }
    }
}
