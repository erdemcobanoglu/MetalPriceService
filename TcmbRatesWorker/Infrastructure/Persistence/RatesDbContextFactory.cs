using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcmbRatesWorker.Infrastructure.Persistence
{
    public sealed class RatesDbContextFactory : IDesignTimeDbContextFactory<RatesDbContext>
    {
        public RatesDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var cs = configuration.GetConnectionString("RatesDb");
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("ConnectionStrings:RatesDb bulunamadı.");

            var optionsBuilder = new DbContextOptionsBuilder<RatesDbContext>();
             
            optionsBuilder.UseSqlServer(cs, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory_TcmbRates", "tcmb"));

            return new RatesDbContext(optionsBuilder.Options);
        }
    }
}
