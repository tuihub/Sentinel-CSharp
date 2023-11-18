using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Sentinel.Plugin.Zdfx
{
    public class Zdfx : IPlugin
    {
        private readonly ILogger? _logger;
        public Zdfx() { }
        public Zdfx(ILogger<Zdfx> logger)
        {
            _logger = logger;
        }

        public string Name => "Zdfx";
        public string Description => "A sentinel plugin that handles zdfx files.";
        public object CommandLineOptions => new Options();

        public IEnumerable<Entry> GetEntries(object objOptions)
        {
            _logger?.LogInformation("Getting options");
            var options = (Options)objOptions;
            var dirPath = options.DirectoryPath;
            if (Directory.Exists(dirPath) == false)
            {
                throw new Exception($"Directory {dirPath} does not exist.");
            }
            var dirPaths = Directory.EnumerateDirectories(dirPath, "*", new EnumerationOptions
            {
                ReturnSpecialDirectories = false,
                RecurseSubdirectories = true,
                MaxRecursionDepth = options.Depth
            });
            var entries = new List<Entry>();
            foreach (var subDirPath in dirPaths)
            {
                GetEntriesFromSubDir(options.PublicUrlPrefix, options.Joiner, entries, subDirPath, subDirPath, new DirectoryInfo(subDirPath).Name);
            }
            return entries;
        }

        private int GetRelativeDepth(string baseDir, string dir)
        {

        }
        private void GetEntriesFromSubDir(string? publicUrlPrefix, char joiner, List<Entry> entries, string baseDirPath, string dirPath, string curName)
        {
            var filePaths = Directory.EnumerateFiles(dirPath, "*", SearchOption.TopDirectoryOnly);
            if (filePaths.Any() == false)
            {
                var subDirPaths = Directory.EnumerateDirectories(dirPath, "*", SearchOption.TopDirectoryOnly);
                foreach (var subDirPath in subDirPaths)
                {
                    GetEntriesFromSubDir(publicUrlPrefix, joiner, entries, baseDirPath, subDirPath, $"{curName}{joiner}{new DirectoryInfo(subDirPath).Name}");
                }
            }
            else
            {
                string publicUrl;
                if (publicUrlPrefix != null)
                    publicUrl = $"{publicUrlPrefix}/{Path.GetRelativePath(baseDirPath, dirPath).Replace(Path.DirectorySeparatorChar, '/')}";
                else
                    publicUrl = dirPath;
                filePaths = Directory.EnumerateFiles(dirPath, "*", SearchOption.AllDirectories);
                using SHA256 sha256 = SHA256.Create();
                var sha256List = new List<byte>();
                var sizeBytes = 0L;
                foreach (var filePath in filePaths)
                {
                    sizeBytes += new FileInfo(filePath).Length;
                    using var fileStream = File.OpenRead(filePath);
                    var fileSha256 = sha256.ComputeHash(fileStream);
                    sha256List.AddRange(fileSha256);
                }
                var combinedSha256 = sha256.ComputeHash(sha256List.ToArray());
                entries.Add(new Entry
                {
                    Name = curName,
                    SizeBytes = sizeBytes,
                    PublicUrl = publicUrl,
                    Sha256 = combinedSha256
                });
            }
        }
    }
}
