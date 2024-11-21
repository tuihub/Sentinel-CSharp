namespace Sentinel.Plugin.Models
{
    public record FileEntryChunk
    {
        public long OffsetBytes { get; init; }
        public long SizeBytes { get; init; }
        public byte[] Sha256 { get; init; }

        public FileEntryChunk(long offsetBytes, long sizeBytes, byte[] sha256)
        {
            OffsetBytes = offsetBytes;
            SizeBytes = sizeBytes;
            Sha256 = sha256 ?? throw new ArgumentNullException(nameof(sha256));
            if (sha256.Length != 32) { throw new ArgumentException("SHA256 must be 32 bytes long."); }
        }
    }
}
