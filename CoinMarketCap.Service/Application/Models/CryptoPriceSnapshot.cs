using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinMarketCap.Service.Application.Models
{
    public sealed class CryptoPriceSnapshot
    {
        public DateTimeOffset CreatedAtUtc { get; init; }
        public string Source { get; set; } = default!;
        public IReadOnlyCollection<CryptoPrice> Prices { get; init; } = Array.Empty<CryptoPrice>();
    }
}
