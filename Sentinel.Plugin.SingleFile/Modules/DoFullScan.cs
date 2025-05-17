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
            try
            {
                var appBinaries = appBinariesIQ.AsEnumerable();

                // appBinariesFilePaths, fsFiles, filesToXX is full paths
                var appBinariesFilePaths = appBinaries.Select(x => Path.Combine(Config.LibraryFolder, x.Files.Single().Path));
                var fsFiles = Directory.EnumerateFiles(Config.LibraryFolder, "*", SearchOption.AllDirectories);
                var filesToRemove = appBinariesFilePaths.Except(fsFiles);
                var filesToAdd = fsFiles.Except(appBinariesFilePaths);
                _logger?.LogDebug($"DoFullScanAsync: Files to remove: {string.Join(", ", filesToRemove)}");
                _logger?.LogDebug($"DoFullScanAsync: Files to add: {string.Join(", ", filesToAdd)}");

                var appBinariesToRecheck = appBinaries.ExceptBy(filesToRemove, x => Path.Combine(Config.LibraryFolder, x.Files.Single().Path));
                _logger?.LogDebug($"DoFullScanAsync: Files to recheck: {string.Join(", ", appBinariesToRecheck.Select(x => x.Files.Single().Path))}");

                var appBinariesToRemove = appBinaries.Where(x => filesToRemove.Select(x => Path.GetRelativePath(Config.LibraryFolder, x)).Contains(x.Files.Single().Path)).ToList();

                var appBinariesToAdd = new List<AppBinary>(filesToAdd.Count());
                foreach (var file in filesToAdd)
                {
                    try
                    {
                        _logger?.LogInformation($"DoFullScanAsync: Adding {file}");
                        var fileEntry = await FileEntryHelper.GetFileEntryAsync(_logger, file, Config.LibraryFolder, Config.ChunkSizeBytes, ct: ct);
                        ct.ThrowIfCancellationRequested();
                        appBinariesToAdd.Add(new AppBinary(Path.GetFileName(file), Path.GetRelativePath(file, file), fileEntry.SizeBytes, [fileEntry], Guid.NewGuid()));
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"Error occurred while adding {file}");
                        continue;
                    }
                }

                var appBinariesToUpdate = new List<AppBinary>();
                foreach (var appBinary in appBinariesToRecheck)
                {
                    try
                    {
                        var fullPath = Path.Combine(Config.LibraryFolder, appBinary.Files.Single().Path);
                        if (Config.ForceCalcDigest || FileEntryHelper.GetLastWriteTimeUtcSec(fullPath) != appBinary.Files.Single().LastWriteUtc)
                        {
                            _logger?.LogDebug("DoFullScanAsync: old LastWriteUtc: " + appBinary.Files.Single().LastWriteUtc.ToString("O") +
                                ", current LastWriteUtc: " + FileEntryHelper.GetLastWriteTimeUtcSec(fullPath).ToString("O"));
                            _logger?.LogInformation("DoFullScanAsync: Rechecking " + fullPath);
                            var fileEntry = await FileEntryHelper.GetFileEntryAsync(_logger, fullPath, Config.LibraryFolder, Config.ChunkSizeBytes, ct: ct);
                            ct.ThrowIfCancellationRequested();
                            if (!fileEntry.Sha256.SequenceEqual(appBinary.Files.Single().Sha256))
                            {
                                _logger?.LogInformation("DoFullScanAsync: Updating " + fullPath + " for its changed SHA256.");
                                appBinariesToUpdate.Add(new AppBinary(appBinary.Name, appBinary.Path, fileEntry.SizeBytes, [fileEntry], Guid.NewGuid()));
                            }
                            else
                            {
                                _logger?.LogInformation("DoFullScanAsync: Updating " + fullPath + " for its changed LastWriteUtc.");
                                appBinariesToUpdate.Add(appBinary with { Files = [appBinary.Files.Single() with { LastWriteUtc = fileEntry.LastWriteUtc }] });
                            }
                        }
                        else
                        {
                            _logger?.LogInformation("DoFullScanAsync: Skipping " + fullPath + " because its LastWriteUtc is not changed.");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error occurred while rechecking " + appBinary.Files.Single().Path);
                        continue;
                    }
                }

                return new ScanChangeResult
                {
                    AppBinariesToRemove = appBinariesToRemove,
                    AppBinariesToAdd = appBinariesToAdd,
                    AppBinariesToUpdate = appBinariesToUpdate
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error occurred while scanning files.");
                return new ScanChangeResult();
            }
        }
    }
}
