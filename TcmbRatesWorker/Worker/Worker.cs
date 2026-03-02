using Microsoft.Extensions.Options;
using TcmbRatesWorker.Application.Services;
using TcmbRatesWorker.Shared.Options;
using TcmbRatesWorker.Shared.Time;

namespace TcmbRatesWorker.Worker
{
    public sealed class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TcmbOptions _options;
        private readonly RatesIngestionService _ingestion;

        public Worker(
            ILogger<Worker> logger,
            IOptions<TcmbOptions> options,
            RatesIngestionService ingestion)
        {
            _logger = logger;
            _options = options.Value;
            _ingestion = ingestion;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);

            while (!stoppingToken.IsCancellationRequested)
            {
                var nowUtc = DateTimeOffset.UtcNow;
                var nextRunUtc = DailySchedule.GetNextRunUtc(nowUtc, tz, _options.RunHour, _options.RunMinute);
                var delay = nextRunUtc - nowUtc;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation(
                        "Next run scheduled at {NextRunUtc} (UTC). Sleeping {Delay}.",
                        nextRunUtc, delay);

                    await Task.Delay(delay, stoppingToken);
                }

                var expectedDate = DailySchedule.GetTodayLocalDate(DateTimeOffset.UtcNow, tz);

                try
                {
                    await _ingestion.IngestTodayAsync(expectedDate, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ingestion failed for {Date}.", expectedDate);
                }
            }
        }
    }
}
