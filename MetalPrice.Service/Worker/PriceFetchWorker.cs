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
                var (nextRun, slot) = GetNextRunWithSlot(now, times);

                var delay = nextRun - DateTime.Now;
                if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(1);

                _logger.LogInformation("Next run at {NextRun} (slot: {Slot}).", nextRun, slot);
                await Task.Delay(delay, stoppingToken);

                try
                {
                    var snapshot = await FetchUsdPricesAsync(slot, stoppingToken);

                    await using var db = await _dbFactory.CreateDbContextAsync(stoppingToken);
                    db.MetalPriceSnapshots.Add(snapshot);

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

        private async Task<List<TimeOnly>> GetTimesAsync(CancellationToken ct)
        {
            var opt = _opt.CurrentValue;

            if (!opt.UseDatabaseSchedule)
                return opt.Times.Select(ScheduleHelper.ParseTimeOnly).ToList();

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var row = await db.ServiceSchedules.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);

            if (row is null)
                return opt.Times.Select(ScheduleHelper.ParseTimeOnly).ToList();

            return new List<TimeOnly>
            {
                ScheduleHelper.ParseTimeOnly(row.MorningTime),
                ScheduleHelper.ParseTimeOnly(row.EveningTime)
            };
        }

        private async Task<MetalPriceSnapshot> FetchUsdPricesAsync(string slot, CancellationToken ct)
        {
            var opt = _opt.CurrentValue;
            var http = _httpClientFactory.CreateClient("metals");

            // DİKKAT: URL'de boşluk yok!
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

                // response zaten USD dönüyor ama yine de payload'dan alalım
                BaseCurrency = resp.currency ?? "USD",

                // Entity alanlarına map
                XAU = gold,
                XAG = silver,
                XPT = platinum,
                XPD = palladium,

                Source = "metals.dev"
            };
        }
         
    }
}
