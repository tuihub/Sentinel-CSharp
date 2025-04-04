namespace Sentinel.Configs
{
    public class SentinelConfig
    {
        public IList<string> Urls { get; set; } = new List<string>();
        public bool NeedToken { get; set; } = false;
        public string GetTokenUrlPath { get; set; } = string.Empty;
        public string DownloadFileUrlPath { get; set; } = string.Empty;
    }
}
