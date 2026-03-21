using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinMarketCap.Service.Infrastructure.Persistence.Entities
{
    public sealed class PriceSnapshotEntity
    {
        public long Id { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }

        public ICollection<CryptoPriceEntity> Prices { get; set; } = new List<CryptoPriceEntity>();
    }
}
