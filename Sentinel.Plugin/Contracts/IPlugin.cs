using Sentinel.Plugin.Configs;
using Sentinel.Plugin.Models;

namespace Sentinel.Plugin.Contracts
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        CommandLineOptionsBase CommandLineOptions { get; set; }
        PluginConfigBase Config { get; set; }

        IEnumerable<SentinelAppBinary> GetSentinelAppBinaries(CommandLineOptionsBase commandLineOptions);
    }
}
