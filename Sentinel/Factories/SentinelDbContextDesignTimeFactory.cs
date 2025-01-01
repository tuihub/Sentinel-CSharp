using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sentinel.Factories
{
    public class SentinelDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SentinelDbContext>
    {
        public SentinelDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SentinelDbContext>();
            optionsBuilder.UseSqlite("Data Source=sentinel.db");

            return new SentinelDbContext(optionsBuilder.Options);
        }
    }
}
