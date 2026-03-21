using CoinMarketCap.Service.Infrastructure.Persistence.Entities; 
using Microsoft.EntityFrameworkCore;

namespace CoinMarketCap.Service.Infrastructure.Persistence
{
    public sealed class CoinMarketCapDbContext : DbContext
    {
        public CoinMarketCapDbContext(DbContextOptions<CoinMarketCapDbContext> options)
            : base(options)
        {
        }

        public DbSet<PriceSnapshotEntity> PriceSnapshots => Set<PriceSnapshotEntity>();
        public DbSet<CryptoPriceEntity> CryptoPrices => Set<CryptoPriceEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoinMarketCapDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
