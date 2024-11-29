using Microsoft.EntityFrameworkCore;
using Sentinel.Plugin.Models;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    [Index(nameof(Guid))]
    public class AppBinary
    {
        [Key]
        public long Id { get; set; }
        public SentinelAppBinary SentinelAppBinary { get; set; } = null!;
        public Guid ReportedGuid { get; set; }
        // relation
        // one-to-many relation (required, to parent)
        public long AppBinaryBaseDirId { get; set; }
        public AppBinaryBaseDir AppBinaryBaseDir { get; set; } = null!;
    }
}
