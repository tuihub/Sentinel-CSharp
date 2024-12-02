using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Helpers;
using Sentinel.Plugin.Models;
using Sentinel.Plugin.Options;

namespace Sentinel.Plugin.SingleFile
{
    public partial class SingleFile : IPlugin
    {
        public IEnumerable<AppBinary> GetAppBinaries(CommandLineOptionsBase objOptions)
        {
            var options = (Options)objOptions;
            var dirPath = options.DirectoryPath;
            if (Directory.Exists(dirPath) == false)
            {
                throw new Exception($"Directory {dirPath} does not exist.");
            }
            var filePaths = Directory.EnumerateFiles(dirPath, "*", SearchOption.AllDirectories);
            var appBinaries = new List<AppBinary>();
            foreach (var filePath in filePaths)
            {
                try
                {
                    _logger?.LogInformation($"Processing {filePath}");
                    var fileEntry = FileEntryHelper.GetFileEntry(_logger, filePath, filePath, options.ChunkSizeBytes, !options.DryRun);
                    appBinaries.Add(new AppBinary(filePath, fileEntry.SizeBytes, [fileEntry]));
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, $"Failed to process {filePath}");
                }
            }
            return appBinaries;
        }
    }
}
