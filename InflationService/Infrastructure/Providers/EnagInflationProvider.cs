using InflationService.Application.Abstractions;
using InflationService.Application.Models;
using InflationService.Infrastructure.Http;
using InflationService.Infrastructure.Parsing;
using Microsoft.Extensions.Logging;

namespace InflationService.Infrastructure.Providers
{
    public sealed class EnagInflationProvider : IInflationProvider
    {
        private const string SourceUrl = "https://enag.ai";

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

            var content = await _httpClient.GetLatestInflationContentAsync(ct);

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("ENAG response content is empty.");
                return null;
            }

            var point = _parser.Parse(content, SourceUrl);

            if (point is null)
            {
                _logger.LogWarning("ENAG content could not be parsed.");
                return null;
            }

            _logger.LogInformation(
                "ENAG data parsed successfully. Period={Year}-{Month:00}, Monthly={MonthlyRate}, Annual={AnnualRate}, Index={IndexValue}",
                point.Year,
                point.Month,
                point.MonthlyRate,
                point.AnnualRate,
                point.IndexValue);

            return point;
        }

        public Task<InflationPoint?> GetByPeriodAsync(int year, int month, CancellationToken ct)
        {
            return GetLatestAsync(ct);
        }
    }
}