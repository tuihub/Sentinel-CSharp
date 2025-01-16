using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;
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

                foreach (var folder in folders)
                {
                    if (PathHelper.GetRelativeDepth(folder, config.LibraryFolder) != config.MinDepth - 1)
                    {
                        _logger?.LogDebug($"DoFullScanAsync: Skipping {folder} because it is not at the minimum depth.");
                        continue;
                    }

                    if (config.ScanPolicy == ScanPolicy.UntilAnyFile)
                    {
                        ScanUntilAnyFileRecursively(folder);

                        #region local function
                        void ScanUntilAnyFileRecursively(string currentFolder)
                        {
                            var currentDirFiles = Directory.EnumerateFiles(currentFolder, "*", SearchOption.TopDirectoryOnly);
                            if (currentDirFiles.Any())
                            {
                                _logger?.LogDebug($"DoFullScanAsync: Files found in {currentFolder}.");
                                return;
                            }
                            else
                            {
                                foreach (var folder in Directory.EnumerateDirectories(currentFolder, "*", SearchOption.TopDirectoryOnly))
                                {
                                    ScanUntilAnyFileRecursively(folder);
                                }
                            }
                        }
                        #endregion
                    }
                    else if (config.ScanPolicy == ScanPolicy.UntilNoFolder)
                    {
                        ScanUntilNoFolderRecursively(folder);

                        #region local function
                        void ScanUntilNoFolderRecursively(string currentFolder)
                        {
                            var currentDirFolders = Directory.EnumerateDirectories(currentFolder, "*", SearchOption.TopDirectoryOnly);
                            if (!currentDirFolders.Any())
                            {
                                _logger?.LogDebug($"DoFullScanAsync: No folders found in {currentFolder}.");
                                var currentDirFiles = Directory.EnumerateFiles(currentFolder, "*", SearchOption.TopDirectoryOnly);
                                if (currentDirFiles.Any())
                                {
                                    _logger?.LogDebug($"DoFullScanAsync: Files found in {currentFolder}.");
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
                                    ScanUntilNoFolderRecursively(folder);
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
