using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Models;

namespace Sentinel.Plugin.Helpers
{
    public static class AppBinaryHelper
    {
        public static bool NeedRescan(ILogger? logger, string libraryFolder, AppBinary dbAppBinary, IEnumerable<string> fsFiles)
        {
            var fsBasePath = Path.Combine(libraryFolder, dbAppBinary.Path);
            if (fsFiles.Count() != dbAppBinary.Files.Count())
            {
                logger?.LogDebug($"NeedRescan: Number of files in {fsBasePath} is different, need to rescan.");
                return true;
            }
            else
            {
                foreach (var dbFile in dbAppBinary.Files)
                {
                    // check if the file exists
                    if (!fsFiles.Contains(Path.Combine(fsBasePath, dbFile.Path)))
                    {
                        logger?.LogDebug($"NeedRescan: File {dbFile.Path} does not exist, need to rescan.");
                        return true;
                    }
                    // check if the file's LastWriteTime is different
                    else if (FileEntryHelper.GetLastWriteTimeUtcSec(Path.Combine(fsBasePath, dbFile.Path)) != dbFile.LastWriteUtc)
                    {
                        logger?.LogDebug($"NeedRescan: File {dbFile.Path} has different LastWriteTime, need to rescan.");
                        return true;
                    }
                    else
                    {
                        logger?.LogDebug($"NeedRescan: File {dbFile.Path} has the same LastWriteTime.");
                    }
                }
            }
            return false;
        }

        public static async Task<AppBinary> GetAppBinaryAsync(ILogger? logger, IEnumerable<string> fsFiles, string libraryFolder, 
            long chunkSizeBytes, string appBinaryRelativePath, CancellationToken ct = default)
        {
            var fileEntries = new List<FileEntry>(fsFiles.Count());
            foreach (var fsFile in fsFiles)
            {
                logger?.LogInformation($"GetAppBinaryAsync: Adding file {fsFile}.");
                fileEntries.Add(await FileEntryHelper.GetFileEntryAsync(logger, fsFile, libraryFolder, chunkSizeBytes, ct: ct));
            }
            return new AppBinary(
                Path.GetFileName(appBinaryRelativePath),
                appBinaryRelativePath,
                fileEntries.Sum(f => f.SizeBytes),
                fileEntries,
                Guid.NewGuid());
        }
    }
}
