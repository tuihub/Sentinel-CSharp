using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Helpers;
using Sentinel.Plugin.Models;

namespace Sentinel.Plugin.SingleFile
{
    public class SingleFile : IPlugin
    {
        private readonly ILogger? _logger;
        public SingleFile() { }
        public SingleFile(ILogger<SingleFile> logger)
        {
            _logger = logger;
        }

        public string Name => "SingleFile";
        public string Description => "A sentinel plugin that handles single files.";
        public object CommandLineOptions => new Options();

        public IEnumerable<SentinelAppBinary> GetSentinelAppBinaries(object objOptions)
        {
            var options = (Options)objOptions;
            var dirPath = options.DirectoryPath;
            if (Directory.Exists(dirPath) == false)
            {
                throw new Exception($"Directory {dirPath} does not exist.");
            }
            var filePaths = Directory.EnumerateFiles(dirPath, "*", SearchOption.AllDirectories);
            var appBinaries = new List<SentinelAppBinary>();
            foreach (var filePath in filePaths)
            {
                try
                {
                    _logger?.LogInformation($"Processing {filePath}");
                    var fileEntry = FileEntryHelper.GetFileEntry(filePath, options.ChunkSizeBytes, _logger, !options.DryRun);
                    appBinaries.Add(new SentinelAppBinary(filePath, fileEntry.SizeBytes, [fileEntry]));
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
