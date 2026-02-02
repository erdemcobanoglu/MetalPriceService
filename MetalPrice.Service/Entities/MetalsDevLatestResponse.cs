using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetalPrice.Service.Entities
{
    public sealed class MetalsDevLatestResponse
    {
        public string? status { get; set; }
        public string? currency { get; set; }
        public string? unit { get; set; }

        public Dictionary<string, decimal> metals { get; set; } = new();

        // başarısız response’larda gelebilir
        public int? error_code { get; set; }
        public string? error_message { get; set; }

        public MetalsDevTimestamps? timestamps { get; set; }
    }
}
