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
                 
                e.Property(x => x.BaseCurrency)
                    .HasMaxLength(8)
                    .IsRequired();

                e.Property(x => x.Source)
                    .HasMaxLength(64)
                    .IsRequired();
                 
                e.Property(x => x.TakenAtDate)
                    .HasComputedColumnSql("CAST([TakenAtUtc] AS date)", stored: true);
                 
                e.Property(x => x.XAU).HasPrecision(38, 18);
                e.Property(x => x.XAG).HasPrecision(38, 18);
                e.Property(x => x.XPT).HasPrecision(38, 18);
                e.Property(x => x.XPD).HasPrecision(38, 18);
                 
                e.Property(x => x.XAU_PerUsd).HasPrecision(38, 18);
                e.Property(x => x.XAG_PerUsd).HasPrecision(38, 18);
                e.Property(x => x.XPT_PerUsd).HasPrecision(38, 18);
                e.Property(x => x.XPD_PerUsd).HasPrecision(38, 18);

                // UNIQUE: one row per day per slot
                e.HasIndex(x => new { x.TakenAtDate, x.RunSlot })
                    .IsUnique() // ✅ bu önemli
                    .HasDatabaseName("UX_MetalPriceSnapshot_TakenAtDate_RunSlot");
            });
        }
    }
}
