using Microsoft.EntityFrameworkCore;
using Sentinel.Attributes;
using Sentinel.Models.Db;
using System.Reflection;

namespace Sentinel
{
    public class SentinelDbContext : DbContext
    {
        public SentinelDbContext() { }
        public SentinelDbContext(DbContextOptions<SentinelDbContext> options) : base(options) { }

        public DbSet<AppBinaryBaseDir> AppBinaryBaseDirs { get; set; } = null!;
        public DbSet<AppBinary> AppBinaries { get; set; } = null!;

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
