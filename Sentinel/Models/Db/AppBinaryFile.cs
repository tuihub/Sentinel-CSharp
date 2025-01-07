using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Sentinel.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Db
{
    [Index(nameof(Path))]
    public class AppBinaryFile
    {
        [Key]
        public long Id { get; set; }
        [MaxLength(4096)]
        public string Path { get; set; } = null!;
        public long SizeBytes { get; set; }
        [IsFixedLength]
        [MaxLength(32)]
        public byte[] Sha256 { get; set; } = null!;
        public ICollection<AppBinaryFileChunk> Chunks { get; set; } = null!;
        public DateTime LastWriteUtc { get; set; }

        // constructor
        public AppBinaryFile() { }
        public AppBinaryFile(Plugin.Models.FileEntry pluginFileEntry)
        {
            Path = pluginFileEntry.Path;
            SizeBytes = pluginFileEntry.SizeBytes;
            Sha256 = pluginFileEntry.Sha256;
            Chunks = pluginFileEntry.Chunks.Select(x => new AppBinaryFileChunk(x)).ToList();
            LastWriteUtc = pluginFileEntry.LastWriteUtc;
        }

        // function
        public Plugin.Models.FileEntry ToPluginModel()
        {
            return new Plugin.Models.FileEntry(Path, SizeBytes, Sha256, Chunks.Select(x => x.ToPluginModel()), LastWriteUtc);
        }
        public TuiHub.Protos.Librarian.Sephirah.V1.AppBinaryFile ToProto()
        {
            return new TuiHub.Protos.Librarian.Sephirah.V1.AppBinaryFile
            {
                Name = System.IO.Path.GetFileName(Path),
                SizeBytes = SizeBytes,
                Sha256 = UnsafeByteOperations.UnsafeWrap(Sha256.AsMemory()),
                ServerFilePath = Path,
                Chunks = { Chunks.Select(x => x.ToProto()) }
            };
        }
    }
}
