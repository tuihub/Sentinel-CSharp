using Sentinel.Plugin.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Plugin.Models
{
    public record FileEntry
    {
        public string FilePath { get; init; }
        public long SizeBytes { get; init; }
        [IsFixedLength]
        [MaxLength(32)]
        public byte[] Sha256 { get; init; }
        public IEnumerable<FileEntryChunk> Chunks { get; init; }
        public DateTime LastWriteUtc { get; init; }

        public FileEntry(string filePath, long sizeBytes, byte[] sha256, IEnumerable<FileEntryChunk> chunks, DateTime lastWriteUtc)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            SizeBytes = sizeBytes;
            Sha256 = sha256 ?? throw new ArgumentNullException(nameof(sha256));
            if (sha256.Length != 32) { throw new ArgumentException("SHA256 must be 32 bytes long."); }
            Chunks = chunks ?? throw new ArgumentNullException(nameof(chunks));
            LastWriteUtc = lastWriteUtc;
        }
    }
}
