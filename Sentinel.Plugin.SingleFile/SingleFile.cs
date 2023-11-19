using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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

        public IEnumerable<Entry> GetEntries(object objOptions)
        {
            var options = (Options)objOptions;
            var dirPath = options.DirectoryPath;
            if (Directory.Exists(dirPath) == false)
            {
                throw new Exception($"Directory {dirPath} does not exist.");
            }
            var filePaths = Directory.EnumerateFiles(dirPath, "*", SearchOption.AllDirectories);
            var entries = new List<Entry>();
            foreach (var filePath in filePaths)
            {
                try
                {
                    _logger?.LogInformation($"Adding {filePath}");
                    var fileInfo = new FileInfo(filePath);
                    using var fileStream = File.OpenRead(filePath);
                    using SHA256 sha256 = SHA256.Create();
                    _logger?.LogInformation($"Computing hash for {filePath}");
                    var fileSha256 = sha256.ComputeHash(fileStream);
                    string publicUrl;
                    if (options.PublicUrlPrefix != null)
                        publicUrl = $"{options.PublicUrlPrefix}/{Path.GetRelativePath(dirPath, filePath).Replace(Path.DirectorySeparatorChar, '/')}";
                    else
                        publicUrl = filePath;
                    entries.Add(new Entry
                    {
                        Name = Path.GetFileNameWithoutExtension(fileInfo.Name),
                        SizeBytes = fileInfo.Length,
                        PublicUrl = publicUrl,
                        Sha256 = fileSha256
                    });
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, $"Failed to process {filePath}");
                }
            }
            return entries;
        }
    }
}
