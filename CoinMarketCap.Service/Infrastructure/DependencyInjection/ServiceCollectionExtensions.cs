using CoinMarketCap.Service.Application.Abstractions;
using CoinMarketCap.Service.Application.Services;
using CoinMarketCap.Service.Infrastructure.Http;
using CoinMarketCap.Service.Infrastructure.Persistence;
using CoinMarketCap.Service.Infrastructure.Persistence.Repositories;
using CoinMarketCap.Service.Shared.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoinMarketCap.Service.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("RatesDb");

            services.AddDbContext<CoinMarketCapDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            services.AddScoped<IPriceRepository, EfPriceRepository>();
            services.AddScoped<PriceIngestionService>();

            services.AddHttpClient<ICoinMarketCapClient, CoinMarketCapClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<CoinMarketCapOptions>>().Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", options.ApiKey);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            return services;
        }
    }
}
