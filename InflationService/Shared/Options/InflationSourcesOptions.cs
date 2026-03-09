using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InflationService.Shared.Options
{
    public sealed class InflationSourcesOptions
    {
        public string TimeZoneId { get; set; } = "Europe/Istanbul";

        public InflationWorkerOptions Tuik { get; set; } = new();

        public InflationWorkerOptions Enag { get; set; } = new();
    }
}
