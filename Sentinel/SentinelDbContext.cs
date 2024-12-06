using Microsoft.EntityFrameworkCore;
using Sentinel.Attributes;
using Sentinel.Configs;
using Sentinel.Models.Db;
using System.Reflection;

namespace Sentinel
{
    public class SentinelDbContext : DbContext
    {
        private readonly SystemConfig _systemConfig;

        public DbSet<AppBinaryBaseDir> AppBinaryBaseDirs { get; set; } = null!;
        public DbSet<AppBinary> AppBinaries { get; set; } = null!;

        public SentinelDbContext(DbContextOptions<SentinelDbContext> options, SystemConfig systemConfig)
            : base(options)
        {
            _systemConfig = systemConfig;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite($"Data Source={_systemConfig.DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    var memberInfo = property.PropertyInfo ?? (MemberInfo?)property.FieldInfo;
                    var attribute = memberInfo?.GetCustomAttribute<IsFixedLengthAttribute>();
                    if (attribute == null) { continue; }
                    if (attribute.IsFixedLength == true)
                    {
                        property.SetIsFixedLength(true);
                    }
                }
            }
        }
    }
}
