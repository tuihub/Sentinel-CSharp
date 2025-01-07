using Sentinel.Plugin.Converters;
using System.Text.Json.Serialization;

namespace Sentinel.Plugin.Models
{
    public record FileEntry
    {
        public string Path { get; init; }
        public long SizeBytes { get; init; }
        public byte[] Sha256 { get; init; }
        public IEnumerable<FileEntryChunk> Chunks { get; init; }
        [JsonConverter(typeof(PyIsoDateTimeJsonConverter))]
        public DateTime LastWriteUtc { get; init; }

        public FileEntry(string path, long sizeBytes, byte[] sha256, IEnumerable<FileEntryChunk> chunks, DateTime lastWriteUtc)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            SizeBytes = sizeBytes;
            Sha256 = sha256 ?? throw new ArgumentNullException(nameof(sha256));
            if (sha256.Length != 32) { throw new ArgumentException("SHA256 must be 32 bytes long."); }
            Chunks = chunks ?? throw new ArgumentNullException(nameof(chunks));
            LastWriteUtc = lastWriteUtc;
        }
    }
}
