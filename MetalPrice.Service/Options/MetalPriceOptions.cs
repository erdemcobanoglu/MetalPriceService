using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetalPrice.Service.Options
{
    public sealed class MetalPriceOptions
    {
        public string ApiKey { get; set; } = "";
        public bool UseDatabaseSchedule { get; set; } = true;
        public string[] Times { get; set; } = Array.Empty<string>();
    }
}
