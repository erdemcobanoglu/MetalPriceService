using InflationService.Application.Abstractions;
using InflationService.Application.Models;
using InflationService.Shared.Options;
using InflationService.Shared.Time;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InflationService.Worker
{
    public sealed class InflationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InflationWorker> _logger;
        private readonly InflationSourcesOptions _options;

        private DateOnly? _lastTuikRunDate;
        private DateOnly? _lastEnagRunDate;

        public InflationWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<InflationSourcesOptions> options,
            ILogger<InflationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TimeZoneInfo timeZone;

            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Invalid timezone configured: {TimeZoneId}", _options.TimeZoneId);
                throw;
            }

            _logger.LogInformation("Inflation worker started. TimeZone={TimeZoneId}", _options.TimeZoneId);

            while (!stoppingToken.IsCancellationRequested)
            {
                var nowUtc = DateTimeOffset.UtcNow;

                var tuikNextRunUtc = _options.Tuik.Enabled
                    ? DailySchedule.GetNextRunUtc(nowUtc, timeZone, _options.Tuik.RunHour, _options.Tuik.RunMinute)
                    : DateTimeOffset.MaxValue;

                var enagNextRunUtc = _options.Enag.Enabled
                    ? DailySchedule.GetNextRunUtc(nowUtc, timeZone, _options.Enag.RunHour, _options.Enag.RunMinute)
                    : DateTimeOffset.MaxValue;

                var nextRunUtc = tuikNextRunUtc <= enagNextRunUtc ? tuikNextRunUtc : enagNextRunUtc;
                var delay = nextRunUtc - nowUtc;

                if (delay > TimeSpan.Zero && nextRunUtc != DateTimeOffset.MaxValue)
                {
                    _logger.LogInformation(
                        "Next inflation run scheduled at {NextRunUtc}. Waiting {Delay}. TuikNext={TuikNextRunUtc}, EnagNext={EnagNextRunUtc}",
                        nextRunUtc,
                        delay,
                        tuikNextRunUtc == DateTimeOffset.MaxValue ? null : tuikNextRunUtc,
                        enagNextRunUtc == DateTimeOffset.MaxValue ? null : enagNextRunUtc);

                    await Task.Delay(delay, stoppingToken);
                }
                else if (nextRunUtc == DateTimeOffset.MaxValue)
                {
                    _logger.LogWarning("All inflation sources are disabled. Worker will check again in 5 minutes.");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }

                nowUtc = DateTimeOffset.UtcNow;
                var todayLocal = DailySchedule.GetTodayLocalDate(nowUtc, timeZone);

                if (_options.Tuik.Enabled &&
                    IsDue(nowUtc, timeZone, _options.Tuik.RunHour, _options.Tuik.RunMinute) &&
                    _lastTuikRunDate != todayLocal)
                {
                    await RunIngestionAsync(InflationSourceType.Tuik, stoppingToken);
                    _lastTuikRunDate = todayLocal;
                }

                if (_options.Enag.Enabled &&
                    IsDue(nowUtc, timeZone, _options.Enag.RunHour, _options.Enag.RunMinute) &&
                    _lastEnagRunDate != todayLocal)
                {
                    await RunIngestionAsync(InflationSourceType.Enag, stoppingToken);
                    _lastEnagRunDate = todayLocal;
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            _logger.LogInformation("Inflation worker stopped.");
        }

        private async Task RunIngestionAsync(InflationSourceType source, CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var ingestionService = scope.ServiceProvider.GetRequiredService<IInflationIngestionService>();

                _logger.LogInformation("Starting inflation ingestion for {Source}.", source);

                var result = await ingestionService.IngestAsync(source, ct);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Inflation ingestion completed. Source={Source}, Period={Year}-{Month}, DataFound={DataFound}, Saved={Saved}, Message={Message}",
                        result.Source,
                        result.Year,
                        result.Month,
                        result.DataFound,
                        result.InsertedOrUpdated,
                        result.Message);
                }
                else
                {
                    _logger.LogWarning(
                        "Inflation ingestion finished with issues. Source={Source}, Period={Year}-{Month}, DataFound={DataFound}, Saved={Saved}, Message={Message}",
                        result.Source,
                        result.Year,
                        result.Month,
                        result.DataFound,
                        result.InsertedOrUpdated,
                        result.Message);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("Inflation ingestion cancelled for {Source}.", source);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during inflation ingestion for {Source}.", source);
            }
        }

        private static bool IsDue(DateTimeOffset nowUtc, TimeZoneInfo timeZone, int runHour, int runMinute)
        {
            var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeZone);
            return localNow.Hour == runHour && localNow.Minute == runMinute;
        }
    }
}