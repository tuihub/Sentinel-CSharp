namespace Sentinel.Configs
{
    public class SentinelConfig
    {
        public IList<string> Hostnames { get; set; } = new List<string>();
        public string Scheme { get; set; } = string.Empty;
        public bool NeedToken { get; set; } = false;
        public string GetTokenUrlPath { get; set; } = string.Empty;
        public string DownloadFileUrlPath { get; set; } = string.Empty;
    }
}
