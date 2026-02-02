using MetalPrice.Service.Data;
using MetalPrice.Service.Entities;
using MetalPrice.Service.Helper;
using MetalPrice.Service.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http;
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

                var nextRun = ScheduleHelper.GetNextRun(DateTime.Now, times);
                var delay = nextRun - DateTime.Now;
                if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(1);

                _logger.LogInformation("Next run at {NextRun}.", nextRun);
                await Task.Delay(delay, stoppingToken);

                try
                {
                    var snapshot = await FetchUsdPricesAsync(stoppingToken);

                    await using var db = await _dbFactory.CreateDbContextAsync(stoppingToken);
                    db.MetalPriceSnapshots.Add(snapshot);
                    await db.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("Saved prices at {TakenAtUtc}.", snapshot.TakenAtUtc);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch/save prices.");
                }
            }
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

        private async Task<MetalPriceSnapshot> FetchUsdPricesAsync(CancellationToken ct)
        {
            var opt = _opt.CurrentValue;
            var http = _httpClientFactory.CreateClient("metals");

            // USD bazında istiyorsun -> base=USD (sağlayıcının desteklediği şekilde)
            var url =
                $"https://metals-api.com/api/latest?access_key={Uri.EscapeDataString(opt.ApiKey)}&base=USD&symbols=XAU,XAG,XPT,XPD";

            var resp = await http.GetFromJsonAsync<MetalsApiLatestResponse>(url, ct)
                       ?? throw new InvalidOperationException("Empty response.");

            // Sağlayıcı farklı döndürürse burayı uyarlarsın.
            return new MetalPriceSnapshot
            {
                TakenAtUtc = DateTime.UtcNow,
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
