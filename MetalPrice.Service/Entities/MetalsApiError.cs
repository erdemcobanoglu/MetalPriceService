using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetalPrice.Service.Entities
{
    public sealed class MetalsApiError
    {
        public int code { get; set; }
        public string? type { get; set; }
        public string? info { get; set; }
    }
}
