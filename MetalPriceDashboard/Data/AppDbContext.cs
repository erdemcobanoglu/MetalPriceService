using Microsoft.EntityFrameworkCore;
using MetalPriceDashboard.Models;

namespace MetalPriceDashboard.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<MetalPricePeriodSummary> MetalPricePeriodSummaries => Set<MetalPricePeriodSummary>();
    public DbSet<RatePeriodSummary> RatePeriodSummaries => Set<RatePeriodSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MetalPricePeriodSummary>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_MetalPricePeriodSummary", "dbo");

            entity.Property(x => x.BaseCurrency).HasMaxLength(10);
            entity.Property(x => x.Metal).HasMaxLength(10);
            entity.Property(x => x.PeriodType).HasMaxLength(20);
            entity.Property(x => x.PeriodLabel).HasMaxLength(100);

            entity.Property(x => x.MinPrice).HasColumnType("decimal(18,6)");
            entity.Property(x => x.MaxPrice).HasColumnType("decimal(18,6)");
        });

        modelBuilder.Entity<RatePeriodSummary>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_RatePeriodSummary", "tcmb");

            entity.Property(x => x.CurrencyCode).HasMaxLength(10);
            entity.Property(x => x.PeriodType).HasMaxLength(20);
            entity.Property(x => x.PeriodLabel).HasMaxLength(100);

            entity.Property(x => x.ForexBuyingMin).HasColumnType("decimal(18,6)");
            entity.Property(x => x.ForexBuyingMax).HasColumnType("decimal(18,6)");
            entity.Property(x => x.ForexSellingMin).HasColumnType("decimal(18,6)");
            entity.Property(x => x.ForexSellingMax).HasColumnType("decimal(18,6)");
            entity.Property(x => x.BanknoteBuyingMin).HasColumnType("decimal(18,6)");
            entity.Property(x => x.BanknoteBuyingMax).HasColumnType("decimal(18,6)");
            entity.Property(x => x.BanknoteSellingMin).HasColumnType("decimal(18,6)");
            entity.Property(x => x.BanknoteSellingMax).HasColumnType("decimal(18,6)");
        });
    }
}