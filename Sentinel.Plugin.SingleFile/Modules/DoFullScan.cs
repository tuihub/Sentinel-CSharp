using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Helpers;
using Sentinel.Plugin.Models;
using Sentinel.Plugin.Results;

namespace Sentinel.Plugin.SingleFile
{
    public partial class SingleFile : IPlugin
    {
        public async Task<ScanChangeResult> DoFullScanAsync(IQueryable<AppBinary> appBinariesIQ, CancellationToken ct = default)
        {
            var appBinaries = appBinariesIQ.AsEnumerable();

            // appBinariesFilePaths, fsFiles, filesToXX is full paths
            var appBinariesFilePaths = appBinaries.Select(x => Path.Combine(Config.LibraryFolder, x.Files.Single().Path));
            var fsFiles = Directory.EnumerateFiles(Config.LibraryFolder, "*", SearchOption.AllDirectories);
            var filesToRemove = appBinariesFilePaths.Except(fsFiles);
            var filesToAdd = fsFiles.Except(appBinariesFilePaths);

            var appBinariesToRecheck = appBinaries.ExceptBy(filesToRemove, x => Path.Combine(Config.LibraryFolder, x.Files.Single().Path));

            _logger?.LogDebug($"DoFullScanAsync: Files to remove: {string.Join(", ", filesToRemove)}");
            var appBinariesToRemove = appBinaries.Where(x => filesToRemove.Select(x => Path.GetRelativePath(Config.LibraryFolder, x)).Contains(x.Files.Single().Path)).ToList();

            var appBinariesToAdd = new List<AppBinary>(filesToAdd.Count());
            foreach (var file in filesToAdd)
            {
                _logger?.LogInformation($"DoFullScanAsync: Adding {file}");
                var fileEntry = await FileEntryHelper.GetFileEntryAsync(_logger, file, Config.LibraryFolder, Config.ChunkSizeBytes, ct: ct);
                ct.ThrowIfCancellationRequested();
                appBinariesToAdd.Add(new AppBinary(Path.GetFileName(file), Path.GetRelativePath(file, file), fileEntry.SizeBytes, [fileEntry], Guid.NewGuid()));
            }

            var appBinariesToUpdate = new List<AppBinary>();
            foreach (var appBinary in appBinariesToRecheck)
            {
                var fullPath = Path.Combine(Config.LibraryFolder, appBinary.Path);
                if (Config.ForceCalcDigest || File.GetLastWriteTimeUtc(fullPath) != appBinary.Files.First().LastWriteUtc)
                {
                    _logger?.LogInformation("DoFullScanAsync: Rechecking " + fullPath);
                    var fileEntry = await FileEntryHelper.GetFileEntryAsync(_logger, fullPath, Config.LibraryFolder, Config.ChunkSizeBytes, ct: ct);
                    ct.ThrowIfCancellationRequested();
                    if (!fileEntry.Sha256.SequenceEqual(appBinary.Files.First().Sha256))
                    {
                        appBinariesToUpdate.Add(new AppBinary(appBinary.Name, appBinary.Path, fileEntry.SizeBytes, [fileEntry], Guid.NewGuid()));
                    }
                }
                else
                {
                    _logger?.LogInformation("DoFullScanAsync: Skipping " + fullPath + " because its LastWriteUtc is not changed.");
                }
            }

            return new ScanChangeResult
            {
                AppBinariesToRemove = appBinariesToRemove,
                AppBinariesToAdd = appBinariesToAdd,
                AppBinariesToUpdate = appBinariesToUpdate
            };
        }
    }
}
