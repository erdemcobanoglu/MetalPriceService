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
                    // 1) Schedule oku/parse et
                    var schedule = await GetTimesAsync(stoppingToken);
                    var times = schedule.Times;

                    // 2) Next run hesapla
                    var now = DateTime.Now;
                    var nextRun = ScheduleHelper.GetNextRun(now, times);

                    // Slot'ı deterministik yap: t_HHmm
                    var slot = $"t_{TimeOnly.FromDateTime(nextRun):HHmm}";

                    // Delay'i aynı "now" ile hesapla (drift/negatif riskini azaltır)
                    var delay = nextRun - now;
                    if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(1);

                    _logger.LogInformation(
                        "Schedule computed. Now={Now}, NextRun={NextRun}, Delay={Delay}, Slot={Slot}, Times=[{Times}]",
                        now, nextRun, delay, slot, string.Join(", ", times.Select(t => t.ToString("HH:mm")))
                    );

                    // 3) Bekle (DEBUG/RELEASE farkı yok)
                    try
                    {
                        await Task.Delay(delay, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break; // normal kapanış
                    }

                    // 4) İşlem (scope)
                    using (_logger.BeginScope(new Dictionary<string, object>
                    {
                        ["RunSlot"] = slot,
                        ["NextRunLocal"] = nextRun,
                        ["TakenAtUtc"] = DateTime.UtcNow
                    }))
                    {
                        try
                        {
                            _logger.LogInformation("Fetching prices...");

                            var snapshot = await FetchUsdPricesAsync(slot, stoppingToken);

                            await using var db = await _dbFactory.CreateDbContextAsync(stoppingToken);

                            db.MetalPriceSnapshots.Add(snapshot);

                            await EnsureServiceScheduleExistsAsync(db, stoppingToken, slot, times);

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

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            // SQL Server
            return ContainsSqlError(ex, 2601, 2627);
        }

        private static bool ContainsSqlError(Exception ex, params int[] errorNumbers)
        {
            var inner = ex;

            while (inner != null)
            {
                if (inner is Microsoft.Data.SqlClient.SqlException sqlEx &&
                    errorNumbers.Contains(sqlEx.Number))
                    return true;

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

        /// <summary>
        /// Mevcut tablona dokunmadan: ilk iki time'ı "morning/evening" gibi işaretlemek istersen.
        /// Slotlar artık t_HHmm olduğu için burada karşılaştırmayı times listesi üzerinden yapıyoruz.
        /// </summary>
        private async Task EnsureServiceScheduleExistsAsync(AppDbContext db, CancellationToken ct, string slot, List<TimeOnly> times)
        {
            var nowUtc = DateTime.UtcNow;

            // İlk iki schedule zamanını "morning/evening" diye işaretle (senin mevcut kolonlara uyum için)
            var morningSlot = times.Count >= 1 ? $"t_{times[0]:HHmm}" : null;
            var eveningSlot = times.Count >= 2 ? $"t_{times[1]:HHmm}" : null;

            var morningValue = (morningSlot != null && string.Equals(slot, morningSlot, StringComparison.OrdinalIgnoreCase))
                ? "morning"
                : string.Empty;

            var eveningValue = (eveningSlot != null && string.Equals(slot, eveningSlot, StringComparison.OrdinalIgnoreCase))
                ? "evening"
                : string.Empty;

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
