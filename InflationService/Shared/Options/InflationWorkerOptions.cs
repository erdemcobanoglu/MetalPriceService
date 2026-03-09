using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InflationService.Shared.Options
{
    public sealed class InflationWorkerOptions
    {
        public bool Enabled { get; set; } = true;
        public int RunHour { get; set; }
        public int RunMinute { get; set; }
    }
}
