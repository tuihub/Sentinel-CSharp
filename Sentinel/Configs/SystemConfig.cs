namespace Sentinel.Configs
{
    public class SystemConfig
    {
        public string LibrarianUrl { get; set; } = "http://librarian";
        public string DbPath { get; set; } = "./sentinel.db";
        public string PluginBaseDir { get; set; } = "./plugins";
        public long MaxPbMsgSizeBytes { get; set; } = 4194304;
        public bool ExitOnFirstReportFailure { get; set; } = true;
        public double HeartbeatIntervalSeconds { get; set; } = 60.0;
        public IList<LibraryConfig> LibraryConfigs { get; set; } = [];
        public double LibraryScanIntervalMinutes { get; set; } = 1440;
    }
}
