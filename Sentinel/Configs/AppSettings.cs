namespace Sentinel.Configs
{
    public class AppSettings
    {
        public SystemConfig SystemConfig { get; set; } = null!;
        public IList<LibraryConfig> LibraryConfigs { get; set; } = null!;
    }
}
