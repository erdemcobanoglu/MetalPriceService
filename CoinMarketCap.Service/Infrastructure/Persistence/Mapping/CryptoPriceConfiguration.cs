using CoinMarketCap.Service.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinMarketCap.Service.Infrastructure.Persistence.Mapping
{
    public sealed class CryptoPriceConfiguration : IEntityTypeConfiguration<CryptoPriceEntity>
    {
        public void Configure(EntityTypeBuilder<CryptoPriceEntity> builder)
        {
            builder.ToTable("CryptoPrices");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Symbol)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.ConvertCurrency)
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(x => x.Price)
                .HasPrecision(38, 18)
                .IsRequired();

            builder.Property(x => x.MarketCap)
                .HasPrecision(38, 8);

            builder.Property(x => x.Volume24h)
                .HasPrecision(38, 8);

            builder.Property(x => x.PercentChange1h)
                .HasPrecision(18, 8);

            builder.Property(x => x.PercentChange24h)
                .HasPrecision(18, 8);

            builder.Property(x => x.PercentChange7d)
                .HasPrecision(18, 8);

            builder.Property(x => x.LastUpdatedUtc)
                .IsRequired();

            builder.Property(x => x.Source)
           .HasMaxLength(50);

            builder.HasIndex(x => new { x.Symbol, x.ConvertCurrency });
            builder.HasIndex(x => x.LastUpdatedUtc);
            builder.HasIndex(x => x.SnapshotId);
        }
    }
}
