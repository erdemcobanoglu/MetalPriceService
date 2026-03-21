using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinMarketCap.Service.Shared.Options
{
    public sealed class PollingOptions
    {
        public const string SectionName = "Polling";

        public int IntervalSeconds { get; init; } = 300;
        public bool RunImmediatelyOnStartup { get; init; } = true;
    }
}
