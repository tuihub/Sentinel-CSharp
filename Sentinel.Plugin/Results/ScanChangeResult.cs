using Sentinel.Plugin.Models;

namespace Sentinel.Plugin.Results
{
    public class ScanChangeResult
    {
        public IEnumerable<AppBinary> AppBinariesToRemove { get; set; } = null!;
        public IEnumerable<AppBinary> AppBinariesToAdd { get; set; } = null!;
        public IEnumerable<AppBinary> AppBinariesToUpdate { get; set; } = null!;
    }
}
