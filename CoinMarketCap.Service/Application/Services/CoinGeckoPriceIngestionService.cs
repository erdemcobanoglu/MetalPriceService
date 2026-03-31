using CoinMarketCap.Service.Application.Abstractions;
using CoinMarketCap.Service.Application.Models;
using Microsoft.Extensions.Logging;

namespace CoinMarketCap.Service.Application.Services
{
    public sealed class CoinGeckoPriceIngestionService
    {
        private readonly ICoinGeckoClient _client;
        private readonly IPriceRepository _repository;
        private readonly ILogger<CoinGeckoPriceIngestionService> _logger;

        public CoinGeckoPriceIngestionService(
            ICoinGeckoClient client,
            IPriceRepository repository,
            ILogger<CoinGeckoPriceIngestionService> logger)
        {
            _client = client;
            _repository = repository;
            _logger = logger;
        }

        public async Task IngestAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("CoinGecko price ingestion started.");

            var prices = await _client.GetLatestPricesAsync(cancellationToken);

            if (prices.Count == 0)
            {
                _logger.LogWarning("CoinGecko returned empty price list.");
                return;
            }

            foreach (var price in prices)
            {
                price.Source = "CoinGecko";
            }

            var snapshot = new CryptoPriceSnapshot
            {
                CreatedAtUtc = DateTimeOffset.UtcNow,
                Source = "CoinGecko",
                Prices = prices.ToList()
            };

            await _repository.SaveSnapshotAsync(snapshot, cancellationToken);

            _logger.LogInformation(
                "CoinGecko snapshot saved successfully. Count={Count}, CreatedAtUtc={CreatedAtUtc}",
                snapshot.Prices.Count,
                snapshot.CreatedAtUtc);
        }
    }
}