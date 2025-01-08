using Sentinel.Plugin.Configs;
using Sentinel.Plugin.Models;
using Sentinel.Plugin.Results;

namespace Sentinel.Plugin.Contracts
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        CmdOptionsBase CmdOptions { get; set; }
        ConfigBase Config { get; set; }
        void SetConfig(CmdOptionsBase cmdOptions)
        {
            Config.LibraryFolder = cmdOptions.DirectoryPath;
            Config.ChunkSizeBytes = cmdOptions.ChunkSizeBytes;
            Config.ForceCalcDigest = !cmdOptions.DryRun;
        }

        Task<ScanChangeResult> DoFullScanAsync(IQueryable<AppBinary> appBinaries, CancellationToken ct = default);
        ScanChangeResult DoFullScan(IQueryable<AppBinary> appBinaries)
        {
            return DoFullScanAsync(appBinaries).Result;
        }
    }
}
