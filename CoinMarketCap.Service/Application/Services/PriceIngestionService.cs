using CoinMarketCap.Service.Application.Abstractions;
using CoinMarketCap.Service.Application.Models;
using CoinMarketCap.Service.Shared.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace CoinMarketCap.Service.Application.Services
{
    public sealed class PriceIngestionService
    {
        private readonly ICoinMarketCapClient _client;
        private readonly IPriceRepository _repository;
        private readonly CoinMarketCapOptions _options;
        private readonly ILogger<PriceIngestionService> _logger;

        public PriceIngestionService(
            ICoinMarketCapClient client,
            IPriceRepository repository,
            IOptions<CoinMarketCapOptions> options,
            ILogger<PriceIngestionService> logger)
        {
            _client = client;
            _repository = repository;
            _options = options.Value;
            _logger = logger;
        }

        public async Task IngestAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Price ingestion started. Symbols={Symbols}, ConvertCurrency={ConvertCurrency}",
                string.Join(",", _options.Symbols),
                _options.ConvertCurrency);

            var prices = await _client.GetLatestPricesAsync(
                _options.Symbols,
                _options.ConvertCurrency,
                cancellationToken);

            if (prices.Count == 0)
            {
                _logger.LogWarning("CoinMarketCap returned empty price list.");
                return;
            }

            var snapshot = new PriceSnapshot
            {
                CreatedAtUtc = DateTimeOffset.UtcNow,
                Prices = prices
            };

            await _repository.SaveSnapshotAsync(snapshot, cancellationToken);

            _logger.LogInformation(
                "Snapshot saved successfully. Count={Count}, CreatedAtUtc={CreatedAtUtc}",
                snapshot.Prices.Count,
                snapshot.CreatedAtUtc);
        }
    }
}
