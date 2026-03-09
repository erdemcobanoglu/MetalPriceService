using InflationService.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InflationService.Infrastructure.Persistence.Configurations
{
    public sealed class InflationSeriesEntityConfiguration : IEntityTypeConfiguration<InflationSeriesEntity>
    {
        public void Configure(EntityTypeBuilder<InflationSeriesEntity> builder)
        {
            builder.ToTable("InflationSeries", "inflation");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Source)
                .IsRequired();

            builder.Property(x => x.Year)
                .IsRequired();

            builder.Property(x => x.Month)
                .IsRequired();

            builder.Property(x => x.MonthlyRate)
                .HasPrecision(18, 4);

            builder.Property(x => x.AnnualRate)
                .HasPrecision(18, 4);

            builder.Property(x => x.IndexValue)
                .HasPrecision(18, 4);

            builder.Property(x => x.RetrievedAtUtc)
                .IsRequired();

            builder.Property(x => x.RawSourceUrl)
                .HasMaxLength(1000);

            builder.HasIndex(x => new { x.Source, x.Year, x.Month })
                .IsUnique()
                .HasDatabaseName("UX_InflationSeries_Source_Year_Month");
        }
    }
}
