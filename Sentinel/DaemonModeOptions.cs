using CommandLine;

namespace Sentinel
{
    [Verb("daemon", aliases: ["d"], HelpText = "Start in daemon mode.")]
    public class DaemonModeOptions
    {
    }
}
