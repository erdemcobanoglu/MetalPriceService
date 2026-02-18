using MetalPrice.Service.Abstractions;
using MetalPrice.Service.Helper;

namespace MetalPrice.Service.Worker
{
    public sealed class PriceFetchWorker : BackgroundService
    {
        private readonly IRunScheduleProvider _scheduleProvider;
        private readonly IPriceSnapshotJob _job;
        private readonly ILogger<PriceFetchWorker> _logger;

        public PriceFetchWorker(
            IRunScheduleProvider scheduleProvider,
            IPriceSnapshotJob job,
            ILogger<PriceFetchWorker> logger)
        {
            _scheduleProvider = scheduleProvider;
            _job = job;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PriceFetchWorker started.");

            bool isFirstRun = true;

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var times = (await _scheduleProvider.GetTimesAsync(stoppingToken))
                        .OrderBy(t => t)
                        .ToList();

                    var now = DateTime.Now;
                    var nextRun = ScheduleHelper.GetNextRun(now, times);

                    var runTime = TimeOnly.FromDateTime(nextRun);

                    // ✅ Slot standardı: 2 time varsa morning/evening sabitle.
                    // Ekstra time varsa t_HHmm fallback.
                    string slot;
                    if (times.Count >= 2)
                    {
                        var morningTime = times[0];
                        var eveningTime = times[1];

                        if (runTime == morningTime) slot = "morning";
                        else if (runTime == eveningTime) slot = "evening";
                        else slot = $"t_{runTime:HHmm}";
                    }
                    else
                    {
                        // Tek time varsa anlamlı isim veremeyiz; yine de t_HHmm
                        slot = $"t_{runTime:HHmm}";
                    }

                    var delay = nextRun - now;
                    if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(1);

                    _logger.LogInformation(
                        "Schedule computed. Now={Now}, NextRun={NextRun}, Delay={Delay}, Slot={Slot}, Times=[{Times}]",
                        now, nextRun, delay, slot, string.Join(", ", times.Select(t => t.ToString("HH:mm")))
                    );

                    // ✅ Delay sadece Release'da çalışsın (Debug'da bekleme yok)
#if !DEBUG
            if (!isFirstRun)
                await Task.Delay(delay, stoppingToken);
#endif
                    isFirstRun = false;

                    using (_logger.BeginScope(new Dictionary<string, object>
                    {
                        ["RunSlot"] = slot,
                        ["NextRunLocal"] = nextRun,
                        ["TakenAtUtc"] = DateTime.UtcNow
                    }))
                    {
                        try
                        {
                            await _job.RunOnceAsync(slot, times, stoppingToken);
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            break;
                        }
                        catch (HttpRequestException ex)
                        {
                            _logger.LogError(ex, "HTTP error while fetching prices.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to fetch/save prices.");
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("PriceFetchWorker shutting down (cancellation requested).");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "PriceFetchWorker crashed unexpectedly.");
                throw;
            }
            finally
            {
                _logger.LogInformation("PriceFetchWorker stopped.");
            }
        }

    }
}
