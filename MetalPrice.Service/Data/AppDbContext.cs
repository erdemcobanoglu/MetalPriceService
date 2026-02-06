using MetalPrice.Service.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalPrice.Service.Data
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<MetalPriceSnapshot> MetalPriceSnapshots => Set<MetalPriceSnapshot>();
        public DbSet<ServiceSchedule> ServiceSchedules => Set<ServiceSchedule>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MetalPriceSnapshot>(e =>
            {
                e.Property(x => x.RunSlot)
                    .HasMaxLength(16)
                    .IsRequired();

                // SQL Server computed, persisted date from TakenAtUtc
                e.Property(x => x.TakenAtDate)
                    .HasComputedColumnSql("CAST([TakenAtUtc] AS date)", stored: true);

                // UNIQUE: one row per day per slot
                e.HasIndex(x => new { x.TakenAtDate, x.RunSlot }) 
                    .HasDatabaseName("UX_MetalPriceSnapshot_TakenAtDate_RunSlot");
            });
        }
    }
}
