using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentinel.Plugin.SingleFile
{
    public class SingleFile : IPlugin
    {
        public readonly ILogger? _logger;
        public SingleFile(LoggerFactory factory)
        {
            _logger = factory?.CreateLogger<SingleFile>();
        }

        public string Name => "SingleFile";
        public string Description => "A plugin that handles single files.";
        public object CommandLineOptions => new Options();

        public IEnumerable<Entry> GetEntries()
        {
            return new List<Entry>
            {
                new Entry
                {
                    Name = "HelloWorld.txt",
                    SizeBytes = 13,
                    PublicUrl = "https://example.com/HelloWorld.txt",
                    Sha256 = new byte[32]
                }
            };
        }
    }
}
