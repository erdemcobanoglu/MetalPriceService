using InflationService.Application.Abstractions;
using InflationService.Application.Models;
using InflationService.Infrastructure.Http;
using InflationService.Infrastructure.Parsing;
using Microsoft.Extensions.Logging;

namespace InflationService.Infrastructure.Providers
{
    public sealed class TuikInflationProvider : IInflationProvider
    {
        private const string SourceUrl = "https://data.tuik.gov.tr/Bulten/Index?p=Tuketici-Fiyat-Endeksi";

        private readonly TuikHttpClient _httpClient;
        private readonly TuikInflationParser _parser;
        private readonly ILogger<TuikInflationProvider> _logger;

        public TuikInflationProvider(
            TuikHttpClient httpClient,
            TuikInflationParser parser,
            ILogger<TuikInflationProvider> logger)
        {
            _httpClient = httpClient;
            _parser = parser;
            _logger = logger;
        }

        public InflationSourceType Source => InflationSourceType.Tuik;

        public async Task<InflationPoint?> GetLatestAsync(CancellationToken ct)
        {
            _logger.LogInformation("Fetching latest TÜİK inflation data.");

            var content = await _httpClient.GetLatestInflationContentAsync(ct);

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("TÜİK response content is empty.");
                return null;
            }

            var point = _parser.Parse(content, SourceUrl);

            if (point is null)
            {
                _logger.LogWarning("TÜİK content could not be parsed.");
                return null;
            }

            _logger.LogInformation(
                "TÜİK data parsed successfully. Period={Year}-{Month:00}, Monthly={MonthlyRate}, Annual={AnnualRate}, Index={IndexValue}",
                point.Year,
                point.Month,
                point.MonthlyRate,
                point.AnnualRate,
                point.IndexValue);

            return point;
        }

        public Task<InflationPoint?> GetByPeriodAsync(int year, int month, CancellationToken ct)
        {
            // İlk fazda kaynakta tarih bazlı endpoint kullanılmadığı için latest üzerinden gidiyoruz.
            return GetLatestAsync(ct);
        }
    }
}