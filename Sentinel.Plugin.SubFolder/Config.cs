using Sentinel.Plugin.Configs;

namespace Sentinel.Plugin.SubFolder
{
    public class Config : ConfigBase
    {
        private int _maxDepth = 1;
        public int MinDepth
        {
            get => _maxDepth;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "MinDepth must be greater than or equal to 1.");
                }
                _maxDepth = value;
            }
        }
        public ScanPolicy ScanPolicy { get; set; } = ScanPolicy.UntilAnyFile;
    }
}
