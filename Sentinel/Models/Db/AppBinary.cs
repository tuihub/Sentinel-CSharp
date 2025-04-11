using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sentinel.Models.Db
{
    [Index(nameof(Path))]
    [Index(nameof(Guid))]
    public class AppBinary
    {
        [Key]
        public long Id { get; set; }
        [MaxLength(511)]
        public string Name { get; set; } = null!;
        [MaxLength(4095)]
        public string Path { get; set; } = null!;
        public long SizeBytes { get; set; }
        public ICollection<AppBinaryFile> Files { get; set; } = null!;
        public Guid Guid { get; set; }

        // relation
        // one-to-many relation (required, to parent)
        [JsonIgnore]
        public long AppBinaryBaseDirId { get; set; }
        [JsonIgnore]
        public AppBinaryBaseDir AppBinaryBaseDir { get; set; } = null!;

        // constructor
        public AppBinary() { }
        public AppBinary(Plugin.Models.AppBinary pluginAppBinary, long appBinaryBaseDirId)
        {
            Name = pluginAppBinary.Name;
            Path = pluginAppBinary.Path;
            SizeBytes = pluginAppBinary.SizeBytes;
            Files = pluginAppBinary.Files.Select(x => new AppBinaryFile(x)).ToList();
            Guid = pluginAppBinary.Guid;
            AppBinaryBaseDirId = appBinaryBaseDirId;
        }

        // function
        public Plugin.Models.AppBinary ToPluginModel()
        {
            return new Plugin.Models.AppBinary(Name, Path, SizeBytes, Files.Select(x => x.ToPluginModel()), Guid);
        }
        public TuiHub.Protos.Librarian.Sephirah.V1.Sentinel.SentinelLibraryAppBinary ToPB(bool needToken)
        {
            return new TuiHub.Protos.Librarian.Sephirah.V1.Sentinel.SentinelLibraryAppBinary
            {
                SentinelLibraryId = Id,
                SentinelGeneratedId = Guid.ToString(),
                SizeBytes = SizeBytes,
                NeedToken = needToken,
                Files = { Files.Select(x => x.ToPB()) },

                Name = Name,
                //Version = string.Empty,
                //Developer = string.Empty,
                //Publisher = string.Empty,
            };
        }
    }
}
