using CommandLine;

namespace Sentinel
{
    [Verb("daemon", aliases: ["d"], HelpText = "Read configuration and run in daemon mode.")]
    public class DaemonOptions
    {
        [Option('n', "no-report", Required = false, Default = false, HelpText = "Logging to console only instead of reporting to server.")]
        public bool NoReportToServer { get; set; }
    }
}
