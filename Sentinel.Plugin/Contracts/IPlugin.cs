using Sentinel.Plugin.Models;

namespace Sentinel.Plugin.Contracts
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        object CommandLineOptions { get; }

        IEnumerable<SentinelAppBinary> GetSentinelAppBinaries(object options);
    }
}
