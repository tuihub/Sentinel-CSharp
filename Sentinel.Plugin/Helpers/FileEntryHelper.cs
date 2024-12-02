using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Models;
using System.Security.Cryptography;

namespace Sentinel.Plugin.Helpers
{
    public static class FileEntryHelper
    {
        public static FileEntry GetFileEntry(ILogger? logger, string fileFullPath, string basePath, long chunkSizeBytes, bool calcSha256 = true, int bufferSizeBytes = 8192)
        {
            logger?.LogInformation($"GetFileEntry: Processing {fileFullPath}");
            using var fileStream = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read);
            long fileSize = fileStream.Length;
            if (chunkSizeBytes % bufferSizeBytes != 0) { throw new ArgumentException("Chunk size must be a multiple of buffer size."); }
            int chunkCount = (int)Math.Ceiling((double)fileSize / chunkSizeBytes);
            var chunks = new List<FileEntryChunk>(chunkCount);
            byte[] fileHash;
            var lastWrite = File.GetLastWriteTimeUtc(fileFullPath);

            if (calcSha256)
            {
                using SHA256 sha256File = SHA256.Create();
                for (int i = 0; i < chunkCount; i++)
                {
                    long offsetBytes = i * chunkSizeBytes;
                    long currentChunkSizeBytes = Math.Min(offsetBytes + chunkSizeBytes, fileSize) - offsetBytes;

                    logger?.LogInformation($"GetFileEntry: Processing chunk {i + 1} / {chunkCount} of {fileFullPath}, ChunkSizeBytes = {currentChunkSizeBytes}");
                    using SHA256 sha256Chunk = SHA256.Create();
                    byte[] buffer = new byte[bufferSizeBytes];
                    long bytesRead = 0;
                    while (bytesRead < currentChunkSizeBytes)
                    {
                        int readSize = (int)Math.Min(buffer.Length, currentChunkSizeBytes - bytesRead);
                        int read = fileStream.Read(buffer, 0, readSize);
                        if (read == 0)
                        {
                            break;
                        }
                        sha256Chunk.TransformBlock(buffer, 0, read, null, 0);
                        sha256File.TransformBlock(buffer, 0, read, null, 0);
                        bytesRead += read;
                    }
                    sha256Chunk.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    byte[] chunkHash = sha256Chunk.Hash ?? new byte[32];
                    chunks.Add(new FileEntryChunk(offsetBytes, currentChunkSizeBytes, chunkHash));
                }
                sha256File.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                fileHash = sha256File.Hash ?? new byte[32];
            }
            // dry run, not calculating SHA256 checksum
            else
            {
                for (int i = 0; i < chunkCount; i++)
                {
                    long offsetBytes = i * chunkSizeBytes;
                    long endBytes = Math.Min(offsetBytes + chunkSizeBytes, fileSize);
                    long currentChunkSize = endBytes - offsetBytes;
                    chunks.Add(new FileEntryChunk(offsetBytes, currentChunkSize, new byte[32]));
                }
                fileHash = new byte[32];
            }
            return new FileEntry(Path.GetRelativePath(basePath, fileFullPath), fileSize, fileHash, chunks, lastWrite);
        }

        public static FileEntry GetFileEntry(string fileFullPath, string basePath, long chunkSizeBytes, bool calcSha256 = true, int bufferSizeBytes = 8192)
        {
            return GetFileEntry(null, fileFullPath, basePath, chunkSizeBytes, calcSha256, bufferSizeBytes);
        }
    }
}
