using InflationService.Application.Abstractions;
using InflationService.Application.Models;
using InflationService.Infrastructure.Http;
using InflationService.Infrastructure.Parsing;
using Microsoft.Extensions.Logging;

namespace InflationService.Infrastructure.Providers
{
    public sealed class TuikInflationProvider : IInflationProvider
    {
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

            var candidateUrls = _httpClient.GetCandidateUrls();

            foreach (var url in candidateUrls)
            {
                var content = await _httpClient.GetContentAsync(url, ct);

                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning("TÜİK response content is empty for {Url}", url);
                    continue;
                }

                var point = _parser.Parse(content, url);

                if (point is null)
                {
                    _logger.LogWarning("TÜİK content could not be parsed from {Url}", url);
                    continue;
                }

                _logger.LogInformation(
                    "TÜİK data parsed successfully. Period={Year}-{Month:00}, Monthly={MonthlyRate}, Annual={AnnualRate}, Index={IndexValue}, Url={Url}",
                    point.Year,
                    point.Month,
                    point.MonthlyRate,
                    point.AnnualRate,
                    point.IndexValue,
                    url);

                return point;
            }

            _logger.LogWarning("No parseable TÜİK inflation data found from any candidate URL.");
            return null;
        }

        public Task<InflationPoint?> GetByPeriodAsync(int year, int month, CancellationToken ct)
        {
            // İlk fazda kaynakta tarih bazlı endpoint kullanılmadığı için latest üzerinden gidiyoruz.
            return GetLatestAsync(ct);
        }
    }
}