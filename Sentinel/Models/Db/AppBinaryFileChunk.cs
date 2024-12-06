using Sentinel.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Db
{
    public class AppBinaryFileChunk
    {
        public long OffsetBytes { get; set; }
        public long SizeBytes { get; set; }
        [IsFixedLength]
        [MaxLength(32)]
        public byte[] Sha256 { get; set; } = null!;

        // function
        public Plugin.Models.FileEntryChunk ToPluginModel()
        {
            return new Plugin.Models.FileEntryChunk(OffsetBytes, SizeBytes, Sha256);
        }
    }
}
