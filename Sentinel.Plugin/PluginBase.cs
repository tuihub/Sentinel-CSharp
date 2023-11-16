using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Models;
using Sentinel.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentinel
{
    public abstract class PluginBase : IPlugin
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public object CommandLineOptions
        {
            get => new OptionsBase();
        }

        public abstract IEnumerable<Entry> GetEntries();
    }
}
