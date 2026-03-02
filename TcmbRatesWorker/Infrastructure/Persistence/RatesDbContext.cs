using Microsoft.EntityFrameworkCore;
using TcmbRatesWorker.Infrastructure.Persistence.Entities;

namespace TcmbRatesWorker.Infrastructure.Persistence
{
    public sealed class RatesDbContext : DbContext
    {
        public RatesDbContext(DbContextOptions<RatesDbContext> options) : base(options) { }

        public DbSet<RateEntity> Rates => Set<RateEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ✅ Tüm tabloları "tcmb" şemasında tut (izolasyon)
            modelBuilder.HasDefaultSchema("tcmb");

            var e = modelBuilder.Entity<RateEntity>();

            // ✅ Artık tablo adı "tcmb.Rates" olacak
            e.ToTable("Rates");

            e.HasKey(x => x.Id);

            e.Property(x => x.CurrencyCode)
                .HasMaxLength(3)
                .IsRequired();

            e.Property(x => x.RateDate)
                .IsRequired();

            e.Property(x => x.Unit)
                .IsRequired();

            e.HasIndex(x => new { x.RateDate, x.CurrencyCode })
                .IsUnique();

            e.Property(x => x.CreatedAtUtc)
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();
        }
    }
}