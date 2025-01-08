using CommandLine;

namespace Sentinel
{
    [Verb("daemon", aliases: ["d"], HelpText = "Read configuration and run in daemon mode.")]
    public class DaemonOptions
    {
    }
}
