using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinMarketCap.Service.Application.Models
{
    public sealed class CryptoPrice
    {
        public string Symbol { get; init; } = default!;
        public string Name { get; init; } = default!;
        public string ConvertCurrency { get; init; } = default!;

        public decimal Price { get; init; }
        public decimal? MarketCap { get; init; }
        public decimal? Volume24h { get; init; }
        public decimal? PercentChange1h { get; init; }
        public decimal? PercentChange24h { get; init; }
        public decimal? PercentChange7d { get; init; }
        public DateTimeOffset LastUpdatedUtc { get; init; }
    }
}
