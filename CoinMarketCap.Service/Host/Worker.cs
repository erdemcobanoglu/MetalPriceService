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
                await Task.Delay(
                    TimeSpan.FromSeconds(_pollingOptions.IntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunOnceSafeAsync(stoppingToken);
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
}