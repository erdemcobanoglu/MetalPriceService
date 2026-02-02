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
                    // Same day + same slot already exists (restart / retry / double run)
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
            // 2 time bekliyoruz: [0]=morning, [1]=evening
            // Eğer farklıysa, sıraya göre slot üretir.
            var nextRun = ScheduleHelper.GetNextRun(now, times);

            var nextTime = TimeOnly.FromDateTime(nextRun);

            // En yakın match: time listesinde hangi index ise onu slot yap
            var idx = times.FindIndex(t => t == nextTime);

            if (idx == 0) return (nextRun, "morning");
            if (idx == 1) return (nextRun, "evening");

            // fallback: time string
            return (nextRun, $"t_{nextTime:HHmm}");
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            // SQL Server unique constraint/index violation: 2601, 2627
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

            // DB boşsa config fallback
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

            var url =
                $"https://metals-api.com/api/latest?access_key={Uri.EscapeDataString(opt.ApiKey)}&base=USD&symbols=XAU,XAG,XPT,XPD";

            var resp = await http.GetFromJsonAsync<MetalsApiLatestResponse>(url, ct)
                       ?? throw new InvalidOperationException("Empty response.");

            return new MetalPriceSnapshot
            {
                TakenAtUtc = DateTime.UtcNow,
                RunSlot = slot,
                BaseCurrency = "USD",
                XAU = resp.rates["XAU"],
                XAG = resp.rates["XAG"],
                XPT = resp.rates["XPT"],
                XPD = resp.rates["XPD"],
                Source = "metals-api"
            };
        }

        private sealed class MetalsApiLatestResponse
        {
            public Dictionary<string, decimal> rates { get; set; } = new();
        }
    }
}
