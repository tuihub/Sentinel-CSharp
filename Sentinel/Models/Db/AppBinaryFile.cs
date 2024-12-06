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
        public IEnumerable<AppBinaryFileChunk> Chunks { get; set; } = null!;
        public DateTime LastWriteUtc { get; set; }

        // function
        public Plugin.Models.FileEntry ToPluginModel()
        {
            return new Plugin.Models.FileEntry(FilePath, SizeBytes, Sha256, Chunks.Select(x => x.ToPluginModel()), LastWriteUtc);
        }
    }
}
