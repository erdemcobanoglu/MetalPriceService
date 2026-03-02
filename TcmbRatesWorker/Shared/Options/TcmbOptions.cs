using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcmbRatesWorker.Shared.Options
{
    public sealed class TcmbOptions
    { 
        public string TodayUrl { get; set; } = "https://www.tcmb.gov.tr/kurlar/today.xml"; 
        public string TimeZoneId { get; set; } = "Europe/Istanbul";

        // Daily work hours (local timezone)
        public int RunHour { get; set; } = 15;
        public int RunMinute { get; set; } = 40;
          
        public int MaxRetry { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 30;
    }
}
