namespace Sentinel.Configs
{
    public class LibraryConfig
    {
        public string PluginName { get; set; } = string.Empty;
        public string DownloadBasePath { get; set; } = string.Empty;
        public object PluginConfig { get; set; } = null!;
    }
}
