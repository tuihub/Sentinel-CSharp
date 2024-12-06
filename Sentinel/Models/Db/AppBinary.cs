using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Db
{
    [Index(nameof(Path))]
    [Index(nameof(Guid))]
    public class AppBinary
    {
        [Key]
        public long Id { get; set; }
        [MaxLength(4096)]
        public string Path { get; set; } = null!;
        public long SizeBytes { get; set; }
        public IEnumerable<AppBinaryFile> Files { get; set; } = null!;
        public Guid Guid { get; set; }
        // relation
        // one-to-many relation (required, to parent)
        public long AppBinaryBaseDirId { get; set; }
        public AppBinaryBaseDir AppBinaryBaseDir { get; set; } = null!;

        // function
        public Plugin.Models.AppBinary ToPluginModel()
        {
            return new Plugin.Models.AppBinary(Path, SizeBytes, Files.Select(x => x.ToPluginModel()));
        }
    }
}
