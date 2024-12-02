using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Helpers;
using Sentinel.Plugin.Models;
using Sentinel.Plugin.Results;

namespace Sentinel.Plugin.SingleFile
{
    public partial class SingleFile : IPlugin
    {
        public ScanChangeResult DoFullScan(IQueryable<AppBinary> appBinaries)
        {
            var fsFiles = Directory.EnumerateFiles(Config.LibraryFolder, "*", SearchOption.AllDirectories);
            var filesToRemove = appBinaries.Select(x => x.Files.First().FilePath).Except(fsFiles);
            var filesToAdd = fsFiles.Except(appBinaries.Select(x => x.Files.First().FilePath));
            var appBinariesToRecheck = appBinaries.ExceptBy(filesToRemove, x => x.Path);

            _logger?.LogDebug($"DoFullScan: Files to remove: {string.Join(", ", filesToRemove)}");

            var appBinariesToAdd = new List<AppBinary>(filesToAdd.Count());
            foreach (var file in filesToAdd)
            {
                _logger?.LogInformation($"DoFullScan: Adding {file}");
                var fileEntry = FileEntryHelper.GetFileEntry(_logger, file, file, Config.ChunkSizeBytes);
                appBinariesToAdd.Add(new AppBinary(file, fileEntry.SizeBytes, [fileEntry]));
            }

            var appBinariesToUpdate = new List<AppBinary>();
            foreach (var appBinary in appBinariesToRecheck)
            {
                if (Config.ForceCalcDigest || File.GetLastWriteTimeUtc(appBinary.Path) != appBinary.Files.First().LastWriteUtc)
                {
                    _logger?.LogInformation("DoFullScan: Rechecking " + appBinary.Path);
                    var fileEntry = FileEntryHelper.GetFileEntry(_logger, appBinary.Path, appBinary.Path, Config.ChunkSizeBytes);
                    if (fileEntry.Sha256 != appBinary.Files.First().Sha256)
                    {
                        appBinariesToUpdate.Add(new AppBinary(appBinary.Path, fileEntry.SizeBytes, [fileEntry]));
                    }
                }
                else
                {
                    _logger?.LogInformation("DoFullScan: Skipping " + appBinary.Path + " because its LastWriteUtc is not changed.");
                }
            }

            return new ScanChangeResult
            {
                AppBinaryPathsToRemove = filesToRemove,
                AppBinariesToAdd = appBinariesToAdd,
                AppBinariesToUpdate = appBinariesToUpdate
            };
        }
    }
}
