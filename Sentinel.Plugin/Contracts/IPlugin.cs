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
        object ConfigObj
        {
            set
            {
                Config = value as PluginConfigBase
                    ?? throw new ArgumentException(Name + " config conversion failed.");
            }
        }

        IEnumerable<AppBinary> GetAppBinaries(CommandLineOptionsBase commandLineOptions);
        Task<ScanChangeResult> DoFullScanAsync(IQueryable<AppBinary> appBinaries, CancellationToken ct = default);
        ScanChangeResult DoFullScan(IQueryable<AppBinary> appBinaries)
        {
            return DoFullScanAsync(appBinaries).Result;
        }
    }
}
