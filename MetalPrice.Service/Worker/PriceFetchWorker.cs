using MetalPrice.Service.Data;
using MetalPrice.Service.Entities;
using MetalPrice.Service.Helper;
using MetalPrice.Service.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace MetalPrice.Service.Worker
{
    public sealed class PriceFetchWorker : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IOptionsMonitor<MetalPriceOptions> _opt;
        private readonly ILogger<PriceFetchWorker> _logger;

        public PriceFetchWorker(
            IHttpClientFactory httpClientFactory,
            IDbContextFactory<AppDbContext> dbFactory,
            IOptionsMonitor<MetalPriceOptions> opt,
            ILogger<PriceFetchWorker> logger)
        {
            _httpClientFactory = httpClientFactory;
            _dbFactory = dbFactory;
            _opt = opt;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PriceFetchWorker started.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    // 1) Schedule hesapla
                    var times = await GetTimesAsync(stoppingToken);

                    var now = DateTime.Now;
                    var (nextRun, slot) = GetNextRunWithSlot(now, times.Times);

                    var delay = nextRun - DateTime.Now;
                    if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(1);

                    _logger.LogInformation("Next run at {NextRun} (slot: {Slot}).", nextRun, slot);

                    // 2) Bekle (iptal normaldir)
                    try
                    {
#if DEBUG
                        await Task.Delay(TimeSpan.FromMilliseconds(1000), stoppingToken);
#else
                        await Task.Delay(delay, stoppingToken);
#endif
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        // Normal kapanış
                        break;
                    }

                    // 3) İşlem (her tur için scope eklemek çok faydalı)
                    using (_logger.BeginScope(new Dictionary<string, object>
                    {
                        ["RunSlot"] = slot,
                        ["TakenAtUtc"] = DateTime.UtcNow
                    }))
                    {
                        try
                        {
                            _logger.LogInformation("Fetching prices...");

                            var snapshot = await FetchUsdPricesAsync(slot, stoppingToken);

                            await using var db = await _dbFactory.CreateDbContextAsync(stoppingToken);

                            db.MetalPriceSnapshots.Add(snapshot);

                            await EnsureServiceScheduleExistsAsync(db, stoppingToken, slot);

                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation(
                                "Saved prices. TakenAtUtc={TakenAtUtc}, Slot={Slot}, XAU={XAU}, XAG={XAG}, XPT={XPT}, XPD={XPD}",
                                snapshot.TakenAtUtc, snapshot.RunSlot, snapshot.XAU, snapshot.XAG, snapshot.XPT, snapshot.XPD);
                        }
                        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                        {
                            _logger.LogWarning("Snapshot already exists for today (slot: {Slot}). Skipping.", slot);
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            // Normal kapanış
                            break;
                        }
                        catch (HttpRequestException ex)
                        {
                            // Ağ/HTTP hatalarını ayrı görmek çok faydalı
                            _logger.LogError(ex, "HTTP error while fetching prices.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to fetch/save prices.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Worker tamamen düşerse burası log basar
                _logger.LogCritical(ex, "PriceFetchWorker crashed unexpectedly.");
                throw; // Host tarafında da görülsün (istenirse kaldırılabilir)
            }
            finally
            {
                _logger.LogInformation("PriceFetchWorker stopped.");
            }
        }

        private static (DateTime nextRun, string slot) GetNextRunWithSlot(DateTime now, List<TimeOnly> times)
        {
            var nextRun = ScheduleHelper.GetNextRun(now, times);
            var nextTime = TimeOnly.FromDateTime(nextRun);

            var idx = times.FindIndex(t => t == nextTime);

            if (idx == 0) return (nextRun, "morning");
            if (idx == 1) return (nextRun, "evening");

            return (nextRun, $"t_{nextTime:HHmm}");
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            // SQL Server
            if (ContainsSqlError(ex, 2601, 2627))
                return true;
             

            return false;
        }

        private static bool ContainsSqlError(Exception ex, params int[] errorNumbers)
        {
            var inner = ex;

            while (inner != null)
            {
                if (inner is Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    if (errorNumbers.Contains(sqlEx.Number))
                        return true;
                }

                inner = inner.InnerException;
            }

            return false;
        }


        private Task<(List<TimeOnly> Times, bool FromDb)> GetTimesAsync(CancellationToken ct)
        {
            var opt = _opt.CurrentValue;

            var rawTimes = opt.Times ?? Array.Empty<string>();

            var parsed = rawTimes
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(ScheduleHelper.ParseTimeOnly)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            if (parsed.Count == 0)
            {
                parsed = new List<TimeOnly>
                {
                    new TimeOnly(9, 0),
                    new TimeOnly(18, 0)
                };
            }

            return Task.FromResult((parsed, false));
        }

        private async Task<MetalPriceSnapshot> FetchUsdPricesAsync(string slot, CancellationToken ct)
        {
            var opt = _opt.CurrentValue;
            var http = _httpClientFactory.CreateClient("metals");

            if (string.IsNullOrWhiteSpace(opt.ApiKey))
                throw new InvalidOperationException("MetalPrice:ApiKey is empty. appsettings.json kontrol edin.");

            var url =
                $"https://api.metals.dev/v1/latest?api_key={Uri.EscapeDataString(opt.ApiKey)}&currency=USD&unit=toz";

            var resp = await http.GetFromJsonAsync<MetalsDevLatestResponse>(url, ct)
                       ?? throw new InvalidOperationException("Empty response.");

            if (!string.Equals(resp.status, "success", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Metals.dev failed: {resp.error_code} - {resp.error_message}");

            if (!resp.metals.TryGetValue("gold", out var gold) ||
                !resp.metals.TryGetValue("silver", out var silver) ||
                !resp.metals.TryGetValue("platinum", out var platinum) ||
                !resp.metals.TryGetValue("palladium", out var palladium))
            {
                throw new InvalidOperationException("Metals.dev response.metals does not contain expected keys: gold/silver/platinum/palladium.");
            }

            return new MetalPriceSnapshot
            {
                TakenAtUtc = DateTime.UtcNow,
                RunSlot = slot,
                BaseCurrency = resp.currency ?? "USD",
                XAU = gold,
                XAG = silver,
                XPT = platinum,
                XPD = palladium,
                Source = "metals.dev"
            };
        }

        private async Task EnsureServiceScheduleExistsAsync(AppDbContext db, CancellationToken ct, string slot)
        {
            var nowUtc = DateTime.UtcNow;

            var morningValue = string.Equals(slot, "morning", StringComparison.OrdinalIgnoreCase) ? "morning" : string.Empty;
            var eveningValue = string.Equals(slot, "evening", StringComparison.OrdinalIgnoreCase) ? "evening" : string.Empty;

            var updated = await db.ServiceSchedules
                .Where(x => x.Id == 1)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.UpdatedAtUtc, nowUtc)
                    .SetProperty(x => x.MorningTime, morningValue)
                    .SetProperty(x => x.EveningTime, eveningValue), ct);

            if (updated > 0)
            {
                _logger.LogInformation(
                    "ServiceSchedules(Id=1) updated. Slot={Slot}, MorningFlag={MorningFlag}, EveningFlag={EveningFlag}, UpdatedAtUtc={UpdatedAtUtc}",
                    slot, morningValue, eveningValue, nowUtc);
                return;
            }

            db.ServiceSchedules.Add(new ServiceSchedule
            {
                MorningTime = morningValue,
                EveningTime = eveningValue,
                UpdatedAtUtc = nowUtc
            });

            _logger.LogInformation(
                "ServiceSchedules(Id=1) created. Slot={Slot}, MorningFlag={MorningFlag}, EveningFlag={EveningFlag}, UpdatedAtUtc={UpdatedAtUtc}",
                slot, morningValue, eveningValue, nowUtc);
        }
    }
}
