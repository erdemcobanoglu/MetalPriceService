using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CoinMarketCap.Service.Infrastructure.Persistence;

public sealed class CoinMarketCapDbContextFactory : IDesignTimeDbContextFactory<CoinMarketCapDbContext>
{
    public CoinMarketCapDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("RatesDb")
            ?? throw new InvalidOperationException("Connection string 'RatesDb' bulunamadı.");

        var optionsBuilder = new DbContextOptionsBuilder<CoinMarketCapDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new CoinMarketCapDbContext(optionsBuilder.Options);
    }
}