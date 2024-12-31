namespace Sentinel.Configs
{
    public class SystemConfig
    {
        public string LibrarianUrl { get; set; } = "http://librarian";
        public string LibrarianRefreshToken { get; set; } = string.Empty;
        public string DbPath { get; set; } = "./sentinel.db";
        public string PluginBaseDir { get; set; } = "./plugins";
        public IEnumerable<LibraryConfig> LibraryConfigs { get; set; } = Enumerable.Empty<LibraryConfig>();
        public long LibraryScanIntervalMinutes { get; set; } = 1440;
    }
}
