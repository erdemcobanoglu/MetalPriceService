using InflationService.Application.Abstractions;
using InflationService.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InflationService.Application.Services
{
    public sealed class InflationIngestionService : IInflationIngestionService
    {
        private readonly IReadOnlyDictionary<InflationSourceType, IInflationProvider> _providers;
        private readonly IInflationRepository _repository;
        private readonly ILogger<InflationIngestionService> _logger;

        public InflationIngestionService(
            IEnumerable<IInflationProvider> providers,
            IInflationRepository repository,
            ILogger<InflationIngestionService> logger)
        {
            _providers = providers.ToDictionary(x => x.Source, x => x);
            _repository = repository;
            _logger = logger;
        }

        public async Task<InflationIngestionResult> IngestAsync(
            InflationSourceType source,
            CancellationToken ct)
        {
            if (!_providers.TryGetValue(source, out var provider))
            {
                var message = $"Provider not registered for source: {source}";
                _logger.LogError(message);

                return new InflationIngestionResult(
                    Source: source,
                    Year: null,
                    Month: null,
                    Success: false,
                    DataFound: false,
                    InsertedOrUpdated: false,
                    Message: message);
            }

            InflationPoint? point;
            try
            {
                point = await provider.GetLatestAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve inflation data from {Source}.", source);

                return new InflationIngestionResult(
                    Source: source,
                    Year: null,
                    Month: null,
                    Success: false,
                    DataFound: false,
                    InsertedOrUpdated: false,
                    Message: $"Failed to retrieve data from {source}.");
            }

            if (point is null)
            {
                var message = $"No inflation data returned from {source}.";
                _logger.LogWarning(message);

                return new InflationIngestionResult(
                    Source: source,
                    Year: null,
                    Month: null,
                    Success: true,
                    DataFound: false,
                    InsertedOrUpdated: false,
                    Message: message);
            }

            if (point.Year < 2000 || point.Month is < 1 or > 12)
            {
                var message = $"Invalid inflation period returned from {source}: {point.Year}-{point.Month:00}";
                _logger.LogWarning(message);

                return new InflationIngestionResult(
                    Source: source,
                    Year: point.Year,
                    Month: point.Month,
                    Success: false,
                    DataFound: true,
                    InsertedOrUpdated: false,
                    Message: message);
            }

            try
            {
                await _repository.UpsertAsync(point, ct);

                _logger.LogInformation(
                    "Inflation data saved. Source={Source}, Period={Year}-{Month:00}, Monthly={MonthlyRate}, Annual={AnnualRate}, Index={IndexValue}",
                    point.Source,
                    point.Year,
                    point.Month,
                    point.MonthlyRate,
                    point.AnnualRate,
                    point.IndexValue);

                return new InflationIngestionResult(
                    Source: point.Source,
                    Year: point.Year,
                    Month: point.Month,
                    Success: true,
                    DataFound: true,
                    InsertedOrUpdated: true,
                    Message: "Inflation data saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to persist inflation data. Source={Source}, Period={Year}-{Month:00}",
                    point.Source,
                    point.Year,
                    point.Month);

                return new InflationIngestionResult(
                    Source: point.Source,
                    Year: point.Year,
                    Month: point.Month,
                    Success: false,
                    DataFound: true,
                    InsertedOrUpdated: false,
                    Message: "Failed to persist inflation data.");
            }
        }
    }
}

