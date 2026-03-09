using InflationService.Infrastructure.Persistence.Configurations;
using InflationService.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace InflationService.Infrastructure.Persistence
{
    public sealed class InflationDbContext : DbContext
    {
        public InflationDbContext(DbContextOptions<InflationDbContext> options)
            : base(options)
        {
        }

        public DbSet<InflationSeriesEntity> InflationSeries => Set<InflationSeriesEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new InflationSeriesEntityConfiguration());
        }
    }
}