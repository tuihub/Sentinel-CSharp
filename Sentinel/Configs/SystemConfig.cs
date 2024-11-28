using Sentinel.Plugin.Configs;

namespace Sentinel.Configs
{
    public class SystemConfig
    {
        public string PluginBaseDir { get; set; } = string.Empty;
        public IEnumerable<LibraryConfig> LibraryConfigs { get; set; } = Enumerable.Empty<LibraryConfig>();
    }
}
