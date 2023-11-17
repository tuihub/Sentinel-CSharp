using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentinel.Plugin.Models
{
    public class Entry
    {
        public string Name { get; set; } = string.Empty;
        public long SizeBytes { get; set; } = 0;
        public string PublicUrl { get; set; } = string.Empty;
        public byte[] Sha256 { get; set; } = new byte[32];
        public override string ToString()
        {
            return "{ " + 
                   $"Name = {Name}, " +
                   $"SizeBytes = {SizeBytes}, " +
                   $"PublicUrl = {PublicUrl}, " +
                   $"Sha256 = {BitConverter.ToString(Sha256).Replace("-", "")}" +
                   " }";
        }
    }
}
