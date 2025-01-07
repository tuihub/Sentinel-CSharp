namespace Sentinel.Configs
{
    public class SystemConfig
    {
        public string LibrarianUrl { get; set; } = "http://librarian";
        public string LibrarianRefreshToken { get; set; } = string.Empty;
        public string DbPath { get; set; } = "./sentinel.db";
        public string PluginBaseDir { get; set; } = "./plugins";
        public IList<LibraryConfig> LibraryConfigs { get; set; } = new List<LibraryConfig>();
        public double LibraryScanIntervalMinutes { get; set; } = 1440;
    }
}
