using Sentinel.Plugin.Models;
using System.Security.Cryptography;

namespace Sentinel.Plugin.Helpers
{
    public static class FileEntryHelper
    {
        public static FileEntry GetFileEntry(string filePath, long chunkSizeBytes, bool calcSha256 = true, int bufferSizeBytes = 8192)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            long fileSize = fileStream.Length;
            if (chunkSizeBytes % bufferSizeBytes != 0) { throw new ArgumentException("Chunk size must be a multiple of buffer size."); }
            int chunkCount = (int)(fileSize / chunkSizeBytes);
            var chunks = new List<FileEntryChunk>(chunkCount);
            byte[] fileHash;
            var lastWrite = File.GetLastWriteTimeUtc(filePath);

            if (calcSha256)
            {
                using SHA256 sha256File = SHA256.Create();
                for (int i = 0; i < chunkCount; i++)
                {
                    long offsetBytes = i * chunkSizeBytes;
                    long endBytes = Math.Min(offsetBytes + chunkSizeBytes, fileSize);
                    long currentChunkSize = endBytes - offsetBytes;

                    using SHA256 sha256Chunk = SHA256.Create();
                    byte[] buffer = new byte[bufferSizeBytes];
                    long bytesRead = 0;
                    while (bytesRead < currentChunkSize)
                    {
                        int readSize = (int)Math.Min(buffer.Length, currentChunkSize - bytesRead);
                        int read = fileStream.Read(buffer, 0, readSize);
                        if (read == 0)
                        {
                            break;
                        }
                        sha256Chunk.TransformBlock(buffer, 0, read, null, 0);
                        sha256File.TransformBlock(buffer, 0, read, null, 0);
                        bytesRead += read;
                    }
                    byte[] chunkHash = sha256Chunk.TransformFinalBlock(buffer, 0, 0);
                    chunks.Add(new FileEntryChunk(offsetBytes, currentChunkSize, chunkHash));
                }
                fileHash = sha256File.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
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
            return new FileEntry(filePath, fileSize, fileHash, chunks, lastWrite);
        }
    }
}
