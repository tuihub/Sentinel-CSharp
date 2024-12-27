using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Db
{
    [Index(nameof(Name), IsUnique = true)]
    public record AppBinaryBaseDir
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }
}
