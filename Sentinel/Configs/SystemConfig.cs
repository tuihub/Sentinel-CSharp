using Sentinel.Plugin.Configs;

namespace Sentinel.Configs
{
    public class SystemConfig
    {
        public string DbPath { get; set; } = "./sentinel.db";
        public string PluginBaseDir { get; set; } = "./plugins";
        public IEnumerable<LibraryConfig> LibraryConfigs { get; set; } = Enumerable.Empty<LibraryConfig>();
    }
}
