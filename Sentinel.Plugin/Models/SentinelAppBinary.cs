namespace Sentinel.Plugin.Models
{
    public record SentinelAppBinary
    {
        public string FilePath { get; init; }
        public long SizeBytes { get; init; }
        public IEnumerable<FileEntry> Files { get; init; }

        public SentinelAppBinary(string filePath, long sizeBytes, IEnumerable<FileEntry> files)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            SizeBytes = sizeBytes;
            Files = files ?? throw new ArgumentNullException(nameof(files));
        }
    }
}
