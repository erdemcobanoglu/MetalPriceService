using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetalPrice.Service.Entities
{
    public sealed class MetalsApiLatestResponse
    {
        public bool success { get; set; }
        public long timestamp { get; set; }
        public string? date { get; set; }
        public string? @base { get; set; }

        public Dictionary<string, decimal> rates { get; set; } = new();

        public MetalsApiError? error { get; set; }
    }
}
