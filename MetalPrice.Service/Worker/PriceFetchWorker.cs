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
                    var times = await _scheduleProvider.GetTimesAsync(stoppingToken);

                    var now = DateTime.Now;
                    var nextRun = ScheduleHelper.GetNextRun(now, times.ToList());
                    var slot = $"t_{TimeOnly.FromDateTime(nextRun):HHmm}";

                    var delay = nextRun - now;
                    if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(1);

                    _logger.LogInformation(
                        "Schedule computed. Now={Now}, NextRun={NextRun}, Delay={Delay}, Slot={Slot}, Times=[{Times}]",
                        now, nextRun, delay, slot, string.Join(", ", times.Select(t => t.ToString("HH:mm")))
                    );

                    if (!isFirstRun)
                        await Task.Delay(delay, stoppingToken); 

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
                // normal shutdown
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
