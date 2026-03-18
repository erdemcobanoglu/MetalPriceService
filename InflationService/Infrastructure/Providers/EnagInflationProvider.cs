using InflationService.Application.Abstractions;
using InflationService.Application.Models;
using InflationService.Infrastructure.Http;
using InflationService.Infrastructure.Parsing;
using Microsoft.Extensions.Logging;

namespace InflationService.Infrastructure.Providers
{
    public sealed class EnagInflationProvider : IInflationProvider
    {
        private readonly EnagHttpClient _httpClient;
        private readonly EnagInflationParser _parser;
        private readonly ILogger<EnagInflationProvider> _logger;

        public EnagInflationProvider(
            EnagHttpClient httpClient,
            EnagInflationParser parser,
            ILogger<EnagInflationProvider> logger)
        {
            _httpClient = httpClient;
            _parser = parser;
            _logger = logger;
        }

        public InflationSourceType Source => InflationSourceType.Enag;

        public async Task<InflationPoint?> GetLatestAsync(CancellationToken ct)
        {
            _logger.LogInformation("Fetching latest ENAG inflation data.");

            foreach (var url in _httpClient.GetCandidateUrls())
            {
                var content = await _httpClient.GetContentAsync(url, ct);

                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning("ENAG response content is empty for {Url}", url);
                    continue;
                }

                var point = _parser.Parse(content, url);

                if (point is null)
                {
                    _logger.LogWarning("ENAG content could not be parsed from {Url}", url);
                    continue;
                }

                _logger.LogInformation(
                    "ENAG data parsed successfully. Period={Year}-{Month:00}, Monthly={MonthlyRate}, Annual={AnnualRate}, Index={IndexValue}, Url={Url}",
                    point.Year,
                    point.Month,
                    point.MonthlyRate,
                    point.AnnualRate,
                    point.IndexValue,
                    url);

                return point;
            }

            _logger.LogWarning("No parseable ENAG inflation data found from any candidate URL.");
            return null;
        }

        public Task<InflationPoint?> GetByPeriodAsync(int year, int month, CancellationToken ct)
        {
            return GetLatestAsync(ct);
        }
    }
}