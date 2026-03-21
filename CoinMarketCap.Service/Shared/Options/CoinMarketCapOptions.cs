using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinMarketCap.Service.Shared.Options
{
    public sealed class CoinMarketCapOptions
    {
        public const string SectionName = "CoinMarketCap";

        public string BaseUrl { get; init; } = "https://pro-api.coinmarketcap.com";
        public string ApiKey { get; init; } = string.Empty;
        public string[] Symbols { get; init; } = Array.Empty<string>();
        public string ConvertCurrency { get; init; } = "USD";
        public int TimeoutSeconds { get; init; } = 30;
    }
}
