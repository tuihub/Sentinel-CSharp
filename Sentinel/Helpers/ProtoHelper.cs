using TuiHub.Protos.Librarian.Sephirah.V1;

namespace Sentinel.Helpers
{
    public static class ProtoHelper
    {
        public static ReportSentinelInformationRequest.Types.ServerScheme ToServerScheme(string serverScheme)
        {
            return serverScheme.ToLower() switch
            {
                "http" => ReportSentinelInformationRequest.Types.ServerScheme.Http,
                "https" => ReportSentinelInformationRequest.Types.ServerScheme.Https,
                _ => throw new ArgumentException($"Invalid server scheme: {serverScheme}")
            };
        }
    }
}
