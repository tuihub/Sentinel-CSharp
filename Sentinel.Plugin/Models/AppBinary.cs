namespace Sentinel.Plugin.Models
{
    public record AppBinary
    {
        public string Name { get; init; }
        public string Path { get; init; }
        public long SizeBytes { get; init; }
        public IEnumerable<FileEntry> Files { get; init; }
        public Guid Guid { get; init; }

        public AppBinary(string name, string path, long sizeBytes, IEnumerable<FileEntry> files, Guid guid)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            SizeBytes = sizeBytes;
            Files = files ?? throw new ArgumentNullException(nameof(files));
            Guid = guid;
        }

        public string ToFullHumanString(int indent = 0)
        {
            string indentStr = new string('\t', indent);
            string ret = $"{indentStr}{nameof(AppBinary)} {{ Name: {Name}, Path: {Path}, SizeBytes: {SizeBytes}, " +
                $"Guid: {Guid}, Files: [";
            foreach (var file in Files)
            {
                ret += Environment.NewLine + indentStr + file.ToFullHumanString(indent + 1);
            }
            ret += Environment.NewLine + indentStr + "] }";
            return ret;
        }
    }
}
