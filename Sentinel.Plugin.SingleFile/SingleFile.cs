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
        private readonly ILogger? _logger;
        //public SingleFile() { }
        public SingleFile(ILogger<SingleFile> logger)
        {
            _logger = logger;
        }

        public string Name => "SingleFile";
        public string Description => "A plugin that handles single files.";
        public object CommandLineOptions => new Options();

        public IEnumerable<Entry> GetEntries()
        {
            _logger?.LogInformation("Adding entry.");
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
