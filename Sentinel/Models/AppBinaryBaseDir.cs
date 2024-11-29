using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    [Index(nameof(Path))]
    public record AppBinaryBaseDir
    {
        [Key]
        public long Id { get; set; }
        public string Path { get; set; } = string.Empty;
    }
}
