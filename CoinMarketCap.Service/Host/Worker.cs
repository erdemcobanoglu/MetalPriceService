using CoinMarketCap.Service.Application.Services;
using CoinMarketCap.Service.Shared.Options;
using Microsoft.Extensions.Options;

namespace CoinMarketCapWorker.Host;

public sealed class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PollingOptions _pollingOptions;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IServiceScopeFactory scopeFactory,
        IOptions<PollingOptions> pollingOptions,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _pollingOptions = pollingOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CoinMarketCap worker started.");

        if (_pollingOptions.RunImmediatelyOnStartup)
        {
            await RunOnceSafeAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nowUtc = DateTimeOffset.UtcNow;
                var nextRunUtc = GetNextRunUtc(nowUtc, _pollingOptions.DailyRunTimeUtc);
                var delay = nextRunUtc - nowUtc;

                _logger.LogInformation(
                    "Next run scheduled. NowUtc={NowUtc}, NextRunUtc={NextRunUtc}, Delay={Delay}",
                    nowUtc,
                    nextRunUtc,
                    delay);

                await Task.Delay(delay, stoppingToken);

                await RunOnceSafeAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during ingestion.");

                // retry safety
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("CoinMarketCap worker stopped.");
    }

    private async Task RunOnceSafeAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<PriceIngestionService>();

            await ingestionService.IngestAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker cancellation requested.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during ingestion.");
        }
    }

    private static DateTimeOffset GetNextRunUtc(DateTimeOffset nowUtc, TimeSpan dailyRunTimeUtc)
    {
        var todayRun = nowUtc.Date.Add(dailyRunTimeUtc);

        if (todayRun > nowUtc.UtcDateTime)
        {
            return new DateTimeOffset(todayRun, TimeSpan.Zero);
        }

        var tomorrowRun = nowUtc.Date.AddDays(1).Add(dailyRunTimeUtc);
        return new DateTimeOffset(tomorrowRun, TimeSpan.Zero);
    }
}