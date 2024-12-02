using Sentinel.Plugin.Configs;
using Sentinel.Plugin.Models;
using Sentinel.Plugin.Options;
using Sentinel.Plugin.Results;

namespace Sentinel.Plugin.Contracts
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        CommandLineOptionsBase CommandLineOptions { get; set; }
        PluginConfigBase Config { get; set; }

        IEnumerable<AppBinary> GetAppBinaries(CommandLineOptionsBase commandLineOptions);
        ScanChangeResult DoFullScan(IQueryable<AppBinary> appBinaries);
    }
}
