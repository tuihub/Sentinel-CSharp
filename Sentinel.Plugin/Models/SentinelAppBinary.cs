namespace Sentinel.Plugin.Models
{
    public record SentinelAppBinary
    {
        public string Path { get; init; }
        public long SizeBytes { get; init; }
        public IEnumerable<FileEntry> Files { get; init; }

        public SentinelAppBinary(string path, long sizeBytes, IEnumerable<FileEntry> files)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            SizeBytes = sizeBytes;
            Files = files ?? throw new ArgumentNullException(nameof(files));
        }
    }
}
