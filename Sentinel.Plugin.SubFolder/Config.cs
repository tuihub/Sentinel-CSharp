using Sentinel.Plugin.Configs;

namespace Sentinel.Plugin.SubFolder
{
    public class Config : ConfigBase
    {
        private int _minDepth = 1;
        public int MinDepth
        {
            get => _minDepth;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "MinDepth must be greater than or equal to 1.");
                }
                _minDepth = value;
            }
        }
        public ScanPolicy ScanPolicy { get; set; } = ScanPolicy.UntilAnyFile;
    }
}
