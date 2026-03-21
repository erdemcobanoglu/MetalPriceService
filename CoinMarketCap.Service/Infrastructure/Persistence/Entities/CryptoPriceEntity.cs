using CoinMarketCap.Service.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinMarketCap.Service.Infrastructure.Persistence.Entities
{
    public sealed class CryptoPriceEntity
    {
       public long Id { get; set; }

    public long SnapshotId { get; set; }
    public CryptoPriceSnapshotEntity Snapshot { get; set; } = default!;

    public string Symbol { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string ConvertCurrency { get; set; } = default!;
    public int? Rank { get; set; }

    public decimal Price { get; set; }
    public decimal? MarketCap { get; set; }
    public decimal? Volume24h { get; set; }
    public decimal? PercentChange1h { get; set; }
    public decimal? PercentChange24h { get; set; }
    public decimal? PercentChange7d { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }
    }
}
