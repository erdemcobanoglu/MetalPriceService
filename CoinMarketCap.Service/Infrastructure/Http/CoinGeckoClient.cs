using System.Globalization;
using System.Text.Json;
using CoinMarketCap.Service.Application.Abstractions;
using CoinMarketCap.Service.Application.Models;
using CoinMarketCap.Service.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoinMarketCap.Service.Infrastructure.Http
{
    public sealed class CoinGeckoClient : ICoinGeckoClient
    {
        private readonly HttpClient _httpClient;
        private readonly CoinGeckoOptions _options;
        private readonly ILogger<CoinGeckoClient> _logger;

        public CoinGeckoClient(
            HttpClient httpClient,
            IOptions<CoinGeckoOptions> options,
            ILogger<CoinGeckoClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<IReadOnlyList<CryptoPrice>> GetLatestPricesAsync(CancellationToken cancellationToken = default)
        {
            var requestUri =
                 $"{_options.MarketsPath}?" +
                 $"vs_currency={_options.VsCurrency}" +
                 $"&order={_options.Order}" +
                 $"&per_page={_options.PerPage}" +
                 $"&page={_options.Page}" +
                 $"&price_change_percentage=1h,24h,7d";

            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "CoinGecko request failed. StatusCode={StatusCode}, Response={Response}",
                    response.StatusCode,
                    content);

                response.EnsureSuccessStatusCode();
            }

            var items = JsonSerializer.Deserialize<List<CoinGeckoMarketItem>>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (items is null || items.Count == 0)
            {
                return Array.Empty<CryptoPrice>();
            }

            var prices = items
            .Where(x => !string.IsNullOrWhiteSpace(x.Symbol) && x.CurrentPrice.HasValue)
            .Select(x => new CryptoPrice
            {
                Symbol = x.Symbol!.ToUpperInvariant(),
                Name = x.Name ?? x.Symbol!.ToUpperInvariant(),
                ConvertCurrency = _options.VsCurrency.ToUpperInvariant(),
                Price = x.CurrentPrice.Value,
                MarketCap = x.MarketCap,
                Volume24h = x.TotalVolume,
                PercentChange1h = x.PriceChangePercentage1hInCurrency,
                PercentChange24h = x.PriceChangePercentage24h,
                PercentChange7d = x.PriceChangePercentage7dInCurrency,
                LastUpdatedUtc = x.LastUpdated ?? DateTimeOffset.UtcNow,
                Rank = x.MarketCapRank
            })
            .ToList();

            return prices;
        }
    }
}