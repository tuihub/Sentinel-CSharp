using Microsoft.EntityFrameworkCore;
using Sentinel.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Db
{
    [Index(nameof(FilePath))]
    public class AppBinaryFile
    {
        [MaxLength(4096)]
        public string FilePath { get; set; } = null!;
        public long SizeBytes { get; set; }
        [IsFixedLength]
        [MaxLength(32)]
        public byte[] Sha256 { get; set; } = null!;
        public IList<AppBinaryFileChunk> Chunks { get; set; } = null!;
        public DateTime LastWriteUtc { get; set; }

        // constructor
        public AppBinaryFile() { }
        public AppBinaryFile(Plugin.Models.FileEntry pluginFileEntry)
        {
            FilePath = pluginFileEntry.FilePath;
            SizeBytes = pluginFileEntry.SizeBytes;
            Sha256 = pluginFileEntry.Sha256;
            Chunks = pluginFileEntry.Chunks.Select(x => new AppBinaryFileChunk(x)).ToList();
            LastWriteUtc = pluginFileEntry.LastWriteUtc;
        }

        // function
        public Plugin.Models.FileEntry ToPluginModel()
        {
            return new Plugin.Models.FileEntry(FilePath, SizeBytes, Sha256, Chunks.Select(x => x.ToPluginModel()), LastWriteUtc);
        }
    }
}
