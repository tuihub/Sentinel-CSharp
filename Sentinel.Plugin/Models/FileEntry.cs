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

        public virtual bool Equals(FileEntry? other)
        {
            if (other is null) { return false; }
            return Path.Equals(other.Path) &&
                   SizeBytes.Equals(other.SizeBytes) &&
                   Sha256.SequenceEqual(other.Sha256) &&
                   Chunks.SequenceEqual(other.Chunks) &&
                   LastWriteUtc.Equals(other.LastWriteUtc);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Path);
            hash.Add(SizeBytes);
            foreach (var b in Sha256)
            {
                hash.Add(b);
            }
            foreach (var chunk in Chunks)
            {
                hash.Add(chunk);
            }
            hash.Add(LastWriteUtc);
            return hash.ToHashCode();
        }

        public string ToFullHumanString(int indent = 1)
        {
            string indentStr = new string('\t', indent);
            string ret = $"{indentStr}{nameof(FileEntry)} {{ Path: {Path}, SizeBytes: {SizeBytes}, " +
                $"Sha256: {BitConverter.ToString(Sha256).Replace("-", "")}, LastWriteUtc: {LastWriteUtc}, Chunks: [";
            foreach (var chunk in Chunks)
            {
                ret += Environment.NewLine + chunk.ToFullHumanString(indent + 1);
            }
            ret += Environment.NewLine + indentStr + "] }";
            return ret;
        }
    }
}
