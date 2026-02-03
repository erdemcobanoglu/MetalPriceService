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
            while (!stoppingToken.IsCancellationRequested)
            {
                var times = await GetTimesAsync(stoppingToken);

                var now = DateTime.Now;
                var (nextRun, slot) = GetNextRunWithSlot(now, times.Times);

                var delay = nextRun - DateTime.Now;
                if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(1);

                _logger.LogInformation("Next run at {NextRun} (slot: {Slot}).", nextRun, slot);
#if DEBUG
                await Task.Delay(TimeSpan.FromMilliseconds(1000), stoppingToken);
#else
    await Task.Delay(delay, stoppingToken);
#endif


                try
                {
                    var snapshot = await FetchUsdPricesAsync(slot, stoppingToken);

                    await using var db = await _dbFactory.CreateDbContextAsync(stoppingToken);
                    db.MetalPriceSnapshots.Add(snapshot);

                    await EnsureServiceScheduleExistsAsync(db, stoppingToken, slot);

                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Saved prices at {TakenAtUtc} (slot: {Slot}).", snapshot.TakenAtUtc, snapshot.RunSlot);
                }
                catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                {
                    _logger.LogWarning("Snapshot already exists for today (slot: {Slot}). Skipping.", slot);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch/save prices.");
                }
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
            if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                return sqlEx.Number is 2601 or 2627;

            return false;
        }

        private Task<(List<TimeOnly> Times, bool FromDb)> GetTimesAsync(CancellationToken ct)
        {
            var opt = _opt.CurrentValue;

            // Sadece appsettings'ten oku
            var rawTimes = opt.Times ?? Array.Empty<string>();

            var parsed = rawTimes
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(ScheduleHelper.ParseTimeOnly)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            // Hiç geçerli değer yoksa fallback
            if (parsed.Count == 0)
            {
                parsed = new List<TimeOnly>
        {
            new TimeOnly(9, 0),
            new TimeOnly(18, 0)
        };
            }

            // FromDb her zaman false
            return Task.FromResult((parsed, false));
        } 

        private async Task<MetalPriceSnapshot> FetchUsdPricesAsync(string slot, CancellationToken ct)
        {
            var opt = _opt.CurrentValue;
            var http = _httpClientFactory.CreateClient("metals");
             
            var url =
                $"https://api.metals.dev/v1/latest?api_key={Uri.EscapeDataString(opt.ApiKey)}&currency=USD&unit=toz";

            // metals.dev response
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


        private static (string Morning, string Evening) NormalizeTimes(List<TimeOnly> times)
        { 
            var morning = (times.Count >= 1 ? times[0] : new TimeOnly(9, 0)).ToString("HH:mm");
            var evening = (times.Count >= 2 ? times[1] : new TimeOnly(21, 0)).ToString("HH:mm");
            return (morning, evening);
        }


    }
}
