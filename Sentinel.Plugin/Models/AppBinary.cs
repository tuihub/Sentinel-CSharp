namespace Sentinel.Plugin.Models
{
    public record AppBinary
    {
        public string Path { get; init; }
        public long SizeBytes { get; init; }
        public IEnumerable<FileEntry> Files { get; init; }
        public Guid Guid { get; init; }

        public AppBinary(string path, long sizeBytes, IEnumerable<FileEntry> files)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            SizeBytes = sizeBytes;
            Files = files ?? throw new ArgumentNullException(nameof(files));
            Guid = new Guid();
        }
    }
}
