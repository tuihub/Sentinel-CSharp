using CommandLine;

namespace Sentinel
{
    [Verb("daemon", aliases: ["d"], HelpText = "Read configuration and run in daemon mode.")]
    public class DaemonOptions
    {
        [Option('n', "no-report", Required = false, Default = false, HelpText = "Logging to console only instead of reporting to server.")]
        public bool NoReportToServer { get; set; }
        [Option('t', "token", Required = false, HelpText = "Specify a new refresh token to use.")]
        public string? RefreshToken { get; set; }
        [Option('u', "update-token-only", Required = false, Default = false, HelpText = "Only update the refresh token in database and exit.")]
        public bool UpdateTokenOnly { get; set; }
    }
}
