using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinMarketCap.Service.Application.Models
{
    public sealed class PriceSnapshot
    {
        public DateTimeOffset CreatedAtUtc { get; init; }
        public IReadOnlyCollection<CryptoPrice> Prices { get; init; } = Array.Empty<CryptoPrice>();
    }
}
