using Sentinel.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sentinel.Models.Db
{
    public class AppBinaryFileChunk
    {
        [Key]
        public long Id { get; set; }
        public long OffsetBytes { get; set; }
        public long SizeBytes { get; set; }
        [IsFixedLength]
        [MaxLength(32)]
        public byte[] Sha256 { get; set; } = null!;

        // relation
        // one-to-many relation (required, to parent)
        [JsonIgnore]
        public long AppBinaryFileId { get; set; }
        [JsonIgnore]
        public AppBinaryFile AppBinaryFile { get; set; } = null!;

        // constructor
        public AppBinaryFileChunk() { }
        public AppBinaryFileChunk(Plugin.Models.FileEntryChunk pluginFileEntryChunk)
        {
            OffsetBytes = pluginFileEntryChunk.OffsetBytes;
            SizeBytes = pluginFileEntryChunk.SizeBytes;
            Sha256 = pluginFileEntryChunk.Sha256;
        }

        // function
        public Plugin.Models.FileEntryChunk ToPluginModel()
        {
            return new Plugin.Models.FileEntryChunk(OffsetBytes, SizeBytes, Sha256);
        }
    }
}
