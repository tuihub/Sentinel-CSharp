using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Db
{
    public class AuthToken
    {
        [Key]
        public int Id { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
} 