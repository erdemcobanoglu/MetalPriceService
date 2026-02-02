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
            modelBuilder.Entity<ServiceSchedule>().HasKey(x => x.Id);

            // İstersen ilk kuruluma varsayılan schedule seed
            modelBuilder.Entity<ServiceSchedule>().HasData(new ServiceSchedule
            {
                Id = 1,
                MorningTime = "09:00",
                EveningTime = "18:00",
                UpdatedAtUtc = DateTime.UtcNow
            });

            modelBuilder.Entity<MetalPriceSnapshot>().HasIndex(x => x.TakenAtUtc);
        }
    }
}
