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
    public sealed class PriceSnapshotConfiguration : IEntityTypeConfiguration<CryptoPriceSnapshotEntity>
    {
        public void Configure(EntityTypeBuilder<CryptoPriceSnapshotEntity> builder)
        {
            builder.ToTable("PriceSnapshots");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasMany(x => x.Prices)
                .WithOne(x => x.Snapshot)
                .HasForeignKey(x => x.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
