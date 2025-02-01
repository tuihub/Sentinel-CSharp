using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Helpers;
using Sentinel.Plugin.Models;
using Sentinel.Plugin.Results;
using Sentinel.Plugin.SubFolder.Helpers;

namespace Sentinel.Plugin.SubFolder
{
    public partial class SubFolder : IPlugin
    {
        public async Task<ScanChangeResult> DoFullScanAsync(IQueryable<AppBinary> appBinariesIQ, CancellationToken ct = default)
        {
            try
            {
                Config config = (Config as Config)!;
                var appBinaries = appBinariesIQ.AsEnumerable();

                var folders = Directory.EnumerateDirectories(config.LibraryFolder, "*", new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    MaxRecursionDepth = config.MinDepth - 1
                });
                _logger?.LogDebug($"DoFullScanAsync: Folders to scan: {string.Join(", ", folders)}");

                var appBinariesToAdd = new List<AppBinary>();
                var appBinariesToUpdate = new List<AppBinary>();
                var appBinariesToRemove = new List<AppBinary>();
                var appBinariesUpToDateRelPaths = new List<string>();

                foreach (var folder in folders)
                {
                    ct.ThrowIfCancellationRequested();
                    if (PathHelper.GetRelativeDepth(folder, config.LibraryFolder) != config.MinDepth - 1)
                    {
                        _logger?.LogDebug($"DoFullScanAsync: Skipping {folder} because it is not at the minimum depth.");
                        continue;
                    }

                    if (config.ScanPolicy == ScanPolicy.UntilAnyFile)
                    {
                        await ScanUntilAnyFileRecursively(folder);

                        #region local function
                        async Task ScanUntilAnyFileRecursively(string currentFolder)
                        {
                            var currentDirFiles = Directory.EnumerateFiles(currentFolder, "*", SearchOption.TopDirectoryOnly);
                            if (currentDirFiles.Any())
                            {
                                _logger?.LogDebug($"DoFullScanAsync: Files found in {currentFolder}.");
                                var appBinaryRelativePath = Path.GetRelativePath(config.LibraryFolder, currentFolder);
                                var fsFiles = Directory.EnumerateFiles(currentFolder, "*", SearchOption.AllDirectories);
                                if (!appBinaries.Any(ab => ab.Path == appBinaryRelativePath))
                                {
                                    _logger?.LogInformation($"DoFullScanAsync: Adding app binary {appBinaryRelativePath}.");
                                    appBinariesToAdd.Add(await AppBinaryHelper.GetAppBinaryAsync(_logger, fsFiles, Config.LibraryFolder, config.ChunkSizeBytes, appBinaryRelativePath, ct: ct));
                                }
                                else if (AppBinaryHelper.NeedRescan(_logger, config.LibraryFolder, appBinaries.Single(ab => ab.Path == appBinaryRelativePath), fsFiles))
                                {
                                    _logger?.LogInformation($"DoFullScanAsync: App binary {appBinaryRelativePath} needs a rescan.");
                                    appBinariesToUpdate.Add(await AppBinaryHelper.GetAppBinaryAsync(_logger, fsFiles, Config.LibraryFolder, config.ChunkSizeBytes, appBinaryRelativePath, ct: ct));
                                }
                                else
                                {
                                    _logger?.LogInformation($"DoFullScanAsync: App binary {appBinaryRelativePath} is already up to date.");
                                    appBinariesUpToDateRelPaths.Add(appBinaryRelativePath);
                                }
                            }
                            else
                            {
                                foreach (var folder in Directory.EnumerateDirectories(currentFolder, "*", SearchOption.TopDirectoryOnly))
                                {
                                    await ScanUntilAnyFileRecursively(folder);
                                }
                            }
                        }
                        #endregion
                    }
                    else if (config.ScanPolicy == ScanPolicy.UntilNoFolder)
                    {
                        await ScanUntilNoFolderRecursively(folder);

                        #region local function
                        async Task ScanUntilNoFolderRecursively(string currentFolder)
                        {
                            var currentDirFolders = Directory.EnumerateDirectories(currentFolder, "*", SearchOption.TopDirectoryOnly);
                            if (!currentDirFolders.Any())
                            {
                                _logger?.LogDebug($"DoFullScanAsync: No folders found in {currentFolder}.");
                                var appBinaryRelativePath = Path.GetRelativePath(config.LibraryFolder, currentFolder);
                                var currentDirFiles = Directory.EnumerateFiles(currentFolder, "*", SearchOption.TopDirectoryOnly);
                                if (currentDirFiles.Any())
                                {
                                    _logger?.LogDebug($"DoFullScanAsync: Files found in {currentFolder}.");
                                    var fsFiles = Directory.EnumerateFiles(currentFolder, "*", SearchOption.AllDirectories);
                                    if (!appBinaries.Any(ab => ab.Path == appBinaryRelativePath))
                                    {
                                        _logger?.LogInformation($"DoFullScanAsync: Adding app binary {appBinaryRelativePath}.");
                                        appBinariesToAdd.Add(await AppBinaryHelper.GetAppBinaryAsync(_logger, fsFiles, Config.LibraryFolder, config.ChunkSizeBytes, appBinaryRelativePath, ct: ct));
                                    }
                                    else if (AppBinaryHelper.NeedRescan(_logger, config.LibraryFolder, appBinaries.Single(ab => ab.Path == appBinaryRelativePath), fsFiles))
                                    {
                                        _logger?.LogInformation($"DoFullScanAsync: App binary {appBinaryRelativePath} needs a rescan.");
                                        appBinariesToUpdate.Add(await AppBinaryHelper.GetAppBinaryAsync(_logger, fsFiles, Config.LibraryFolder, config.ChunkSizeBytes, appBinaryRelativePath, ct: ct));
                                    }
                                    else
                                    {
                                        _logger?.LogInformation($"DoFullScanAsync: App binary {appBinaryRelativePath} is already up to date.");
                                        appBinariesUpToDateRelPaths.Add(appBinaryRelativePath);
                                    }
                                }
                                else
                                {
                                    _logger?.LogDebug($"DoFullScanAsync: No files found in {currentFolder}, not adding any app binary.");
                                }
                                return;
                            }
                            else
                            {
                                foreach (var folder in currentDirFolders)
                                {
                                    await ScanUntilNoFolderRecursively(folder);
                                }
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(config.ScanPolicy), config.ScanPolicy, "Invalid scan policy.");
                    }
                }

                appBinariesToRemove = appBinaries
                    .Where(ab => !appBinariesUpToDateRelPaths.Contains(ab.Path) &&
                        !appBinariesToAdd.Select(ab => ab.Path).Contains(ab.Path) &&
                        !appBinariesToUpdate.Select(ab => ab.Path).Contains(ab.Path))
                    .ToList();

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
