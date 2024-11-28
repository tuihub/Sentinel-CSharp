using Sentinel.Plugin.Models;

namespace Sentinel.Plugin.Contracts
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        object CommandLineOptions { get; set; }
        object Config { get; set; }

        IEnumerable<SentinelAppBinary> GetSentinelAppBinaries(object options);
    }
}
