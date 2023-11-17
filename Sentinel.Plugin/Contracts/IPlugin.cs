using Sentinel.Plugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentinel.Plugin.Contracts
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        object CommandLineOptions { get; }

        IEnumerable<Entry> GetEntries(object options);
    }
}
