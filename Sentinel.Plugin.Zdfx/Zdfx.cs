using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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
                MaxRecursionDepth = options.Depth - 1
            });
            var entries = new List<Entry>();
            foreach (var subDirPath in dirPaths)
            {
                _logger?.LogInformation($"Processing top dir {subDirPath}");
                var relativeDepth = GetRelativeDepth(dirPath, subDirPath);
                _logger?.LogInformation($"relativeDepth = {relativeDepth}");
                if (relativeDepth == options.Depth)
                    GetEntriesFromSubDir(options.PublicUrlPrefix, options.Joiner, entries, dirPath, subDirPath, new DirectoryInfo(subDirPath).Name);
            }
            return entries;
        }

        private int GetRelativeDepth(string baseDir, string dir)
        {
            return Path.GetRelativePath(baseDir, dir).Count(c => c == Path.DirectorySeparatorChar) + 1;
        }

        private void GetEntriesFromSubDir(string? publicUrlPrefix, char joiner, List<Entry> entries, string baseDirPath, string dirPath, string curName)
        {
            _logger?.LogInformation($"Processing sub dir {dirPath}");
            var filePaths = Directory.EnumerateFiles(dirPath, "*", SearchOption.TopDirectoryOnly);
            if (filePaths.Any() == false)
            {
                _logger?.LogInformation($"Sub dir {dirPath} does not contain any file, searching for its sub dirs");
                var subDirPaths = Directory.EnumerateDirectories(dirPath, "*", SearchOption.TopDirectoryOnly);
                foreach (var subDirPath in subDirPaths)
                {
                    GetEntriesFromSubDir(publicUrlPrefix, joiner, entries, baseDirPath, subDirPath, $"{curName}{joiner}{new DirectoryInfo(subDirPath).Name}");
                }
            }
            else
            {
                filePaths = Directory.EnumerateFiles(dirPath, "*", SearchOption.AllDirectories);
                if (filePaths.Any() == false)
                {
                    _logger?.LogWarning($"Directory {dirPath} is empty.");
                    return;
                }
                _logger?.LogInformation($"Adding dir {dirPath}");
                using SHA256 sha256 = SHA256.Create();
                // concat
                //var sha256List = new List<byte>();
                // xor
                //var sha256BitArray = new BitArray(new byte[32]);
                // add
                var finSha256BitInt = new BigInteger();
                var sizeBytes = 0L;
                string publicUrl;
                if (publicUrlPrefix != null)
                    publicUrl = $"{publicUrlPrefix}/{Path.GetRelativePath(baseDirPath, dirPath).Replace(Path.DirectorySeparatorChar, '/')}";
                else
                    publicUrl = dirPath;
                foreach (var filePath in filePaths)
                {
                    _logger?.LogInformation($"Computing hash for {filePath}");
                    sizeBytes += new FileInfo(filePath).Length;
                    using var fileStream = File.OpenRead(filePath);
                    var fileSha256 = sha256.ComputeHash(fileStream);
                    // concat
                    //sha256List.AddRange(fileSha256);
                    // xor
                    //var fileSha256BitArray = new BitArray(fileSha256);
                    //sha256BitArray.Xor(fileSha256BitArray);
                    // add
                    finSha256BitInt += new BigInteger(fileSha256);
                }
                _logger?.LogInformation($"Computing final hash");
                // concat
                //var finalSha256 = sha256.ComputeHash(sha256List.ToArray());
                // xor
                //var finalSha256 = new byte[32];
                //sha256BitArray.CopyTo(finalSha256, 0);
                // add
                var finalSha256 = new byte[32];
                Array.Copy(finSha256BitInt.ToByteArray(), finalSha256, 32);
                entries.Add(new Entry
                {
                    Name = curName,
                    SizeBytes = sizeBytes,
                    PublicUrl = publicUrl,
                    Sha256 = finalSha256
                });
            }
        }
    }
}
