using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetalPrice.Service.Entities
{
    public sealed class MetalPriceSnapshot
    {
        public long Id { get; set; }
        public DateTime TakenAtUtc { get; set; }

        public string BaseCurrency { get; set; } = "USD"; // sabit USD
        public decimal XAU { get; set; }
        public decimal XAG { get; set; }
        public decimal XPT { get; set; }
        public decimal XPD { get; set; }

        public string Source { get; set; } = "metals-api";
    }
}
