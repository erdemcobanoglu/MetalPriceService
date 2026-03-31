using CoinMarketCap.Service.Application.Abstractions;
using CoinMarketCap.Service.Application.Models;
using CoinMarketCap.Service.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoinMarketCap.Service.Infrastructure.Persistence.Repositories
{
    public sealed class EfPriceRepository : IPriceRepository
    {
        private readonly CoinMarketCapDbContext _dbContext;

        public EfPriceRepository(CoinMarketCapDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveSnapshotAsync(
            CryptoPriceSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            var entity = new CryptoPriceSnapshotEntity
            {
                CreatedAtUtc = snapshot.CreatedAtUtc,
                Source = snapshot.Source,
                Prices = snapshot.Prices.Select(x => new CryptoPriceEntity
                {
                    Symbol = x.Symbol,
                    Name = x.Name,
                    ConvertCurrency = x.ConvertCurrency,
                    Price = x.Price,
                    MarketCap = x.MarketCap,
                    Volume24h = x.Volume24h,
                    PercentChange1h = x.PercentChange1h,
                    PercentChange24h = x.PercentChange24h,
                    PercentChange7d = x.PercentChange7d,
                    LastUpdatedUtc = x.LastUpdatedUtc,
                    Rank = x.Rank,
                    Source = x.Source
                }).ToList()
            };

            _dbContext.PriceSnapshots.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<CryptoPriceSnapshot?> GetLatestSnapshotAsync(
            CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.PriceSnapshots
                .AsNoTracking()
                .Include(x => x.Prices)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity is null)
            {
                return null;
            }

            return new CryptoPriceSnapshot
            {
                CreatedAtUtc = entity.CreatedAtUtc,
                Source = entity.Source,
                Prices = entity.Prices
                    .OrderBy(x => x.Symbol)
                    .Select(x => new CryptoPrice
                    {
                        Symbol = x.Symbol,
                        Name = x.Name,
                        ConvertCurrency = x.ConvertCurrency,
                        Price = x.Price,
                        MarketCap = x.MarketCap,
                        Volume24h = x.Volume24h,
                        PercentChange1h = x.PercentChange1h,
                        PercentChange24h = x.PercentChange24h,
                        PercentChange7d = x.PercentChange7d,
                        LastUpdatedUtc = x.LastUpdatedUtc,
                        Rank = x.Rank,
                        Source = x.Source
                    })
                    .ToArray()
            };
        }
    }
}