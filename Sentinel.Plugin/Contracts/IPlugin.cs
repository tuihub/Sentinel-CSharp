using Sentinel.Plugin.Configs;
using Sentinel.Plugin.Models;
using Sentinel.Plugin.Options;
using Sentinel.Plugin.Results;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sentinel.Plugin.Contracts
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        CommandLineOptionsBase CommandLineOptions { get; set; }
        PluginConfigBase Config { get; set; }
        JsonNode ConfigJsonNode
        {
            set
            {
                Config = JsonSerializer.Deserialize(value, Config.GetType()) as PluginConfigBase
                    ?? throw new ArgumentException(Name + " config deserialization failed.");
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
