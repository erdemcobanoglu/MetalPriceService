using CoinMarketCap.Service.Application.Abstractions;
using CoinMarketCap.Service.Application.Models;
using CoinMarketCap.Service.Infrastructure.Http.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoinMarketCap.Service.Infrastructure.Http
{
    public sealed class CoinMarketCapClient : ICoinMarketCapClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly ILogger<CoinMarketCapClient> _logger;

        public CoinMarketCapClient(
            HttpClient httpClient,
            ILogger<CoinMarketCapClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<CryptoPrice>> GetLatestPricesAsync(
            IReadOnlyCollection<string> symbols,
            string convertCurrency,
            CancellationToken cancellationToken = default)
        {
            var symbolCsv = string.Join(",", symbols);
            var requestUri = $"/v1/cryptocurrency/quotes/latest?symbol=&convert=USD";
               // $"/v1/cryptocurrency/quotes/latest?symbol={Uri.EscapeDataString(symbolCsv)}&convert={Uri.EscapeDataString(convertCurrency)}";

            _logger.LogInformation(
                "Sending CoinMarketCap request. Symbols={Symbols}, ConvertCurrency={ConvertCurrency}",
                symbolCsv,
                convertCurrency);

            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "CoinMarketCap request failed. StatusCode={StatusCode}, Body={Body}",
                    (int)response.StatusCode,
                    body);

                response.EnsureSuccessStatusCode();
            }

            var dto = JsonSerializer.Deserialize<CoinMarketCapLatestQuotesResponseDto>(body, JsonOptions);

            if (dto?.Data is null || dto.Data.Count == 0)
            {
                return Array.Empty<CryptoPrice>();
            }

            var result = new List<CryptoPrice>();

            foreach (var item in dto.Data.Values)
            {
                if (!item.Quote.TryGetValue(convertCurrency, out var quote))
                {
                    continue;
                }

                result.Add(new CryptoPrice
                {
                    Symbol = item.Symbol,
                    Name = item.Name,
                    ConvertCurrency = convertCurrency,
                    Price = quote.Price,
                    MarketCap = quote.MarketCap,
                    Volume24h = quote.Volume24h,
                    PercentChange1h = quote.PercentChange1h,
                    PercentChange24h = quote.PercentChange24h,
                    PercentChange7d = quote.PercentChange7d,
                    LastUpdatedUtc = quote.LastUpdated
                });
            }

            _logger.LogInformation(
                "CoinMarketCap response parsed successfully. Count={Count}",
                result.Count);

            return result;
        }
    }
}
