using MetalPrice.Service.Abstractions;
using MetalPrice.Service.Data;
using MetalPrice.Service.Entities;
using MetalPrice.Service.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace MetalPrice.Service.Services
{
    public sealed class MetalsDevSnapshotJob : IPriceSnapshotJob
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IOptionsMonitor<MetalPriceOptions> _opt;
        private readonly ILogger<MetalsDevSnapshotJob> _logger;

        public MetalsDevSnapshotJob(
            IHttpClientFactory httpClientFactory,
            IDbContextFactory<AppDbContext> dbFactory,
            IOptionsMonitor<MetalPriceOptions> opt,
            ILogger<MetalsDevSnapshotJob> logger)
        {
            _httpClientFactory = httpClientFactory;
            _dbFactory = dbFactory;
            _opt = opt;
            _logger = logger;
        }

        public async Task RunOnceAsync(string slot, IReadOnlyList<TimeOnly> times, CancellationToken ct)
        {
            var snapshot = await FetchUsdPricesAsync(slot, ct);

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.MetalPriceSnapshots.Add(snapshot);

            await UpsertServiceScheduleAsync(db, ct, slot, times);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning("Snapshot already exists for today (slot: {Slot}). Skipping.", slot);
            }
        }

        #region OldVersionv1 
        //private async Task<MetalPriceSnapshot> FetchUsdPricesAsync(string slot, CancellationToken ct)
        //{
        //    var opt = _opt.CurrentValue;
        //    if (string.IsNullOrWhiteSpace(opt.ApiKey))
        //        throw new InvalidOperationException("MetalPrice:ApiKey is empty. appsettings.json kontrol edin.");

        //    var http = _httpClientFactory.CreateClient("metals");

        //    var url =
        //        $"https://metals-api.com/api/latest?access_key={Uri.EscapeDataString(opt.ApiKey)}&base=USD&symbols=XAU,XAG,XPT,XPD";

        //    var resp = await http.GetFromJsonAsync<MetalsDevLatestResponse>(url, ct)
        //               ?? throw new InvalidOperationException("Empty response.");

        //    if (!string.Equals(resp.status, "success", StringComparison.OrdinalIgnoreCase))
        //        throw new InvalidOperationException($"Metals.dev failed: {resp.error_code} - {resp.error_message}");

        //    if (!resp.metals.TryGetValue("gold", out var gold) ||
        //        !resp.metals.TryGetValue("silver", out var silver) ||
        //        !resp.metals.TryGetValue("platinum", out var platinum) ||
        //        !resp.metals.TryGetValue("palladium", out var palladium))
        //    {
        //        throw new InvalidOperationException("Metals.dev response.metals does not contain expected keys: gold/silver/platinum/palladium.");
        //    }

        //    return new MetalPriceSnapshot
        //    {
        //        TakenAtUtc = DateTime.UtcNow,
        //        RunSlot = slot,
        //        BaseCurrency = resp.currency ?? "USD",
        //        XAU = gold,
        //        XAG = silver,
        //        XPT = platinum,
        //        XPD = palladium,
        //        Source = "metals.dev"
        //    };
        //}

        #endregion

        private async Task<MetalPriceSnapshot> FetchUsdPricesAsync(string slot, CancellationToken ct)
        {
            var opt = _opt.CurrentValue;
            if (string.IsNullOrWhiteSpace(opt.ApiKey))
                throw new InvalidOperationException("MetalPrice:ApiKey is empty. appsettings.json kontrol edin.");

            var http = _httpClientFactory.CreateClient("metals");

            var url =
                $"https://metals-api.com/api/latest?access_key={Uri.EscapeDataString(opt.ApiKey)}&base=USD&symbols=USD,XAU,XAG,XPT,XPD,USDXAU,USDXAG,USDXPT,USDXPD";

            var resp = await http.GetFromJsonAsync<MetalsApiLatestResponse>(url, ct)
                       ?? throw new InvalidOperationException("Empty response.");

            if (!resp.success)
                throw new InvalidOperationException($"metals-api.com failed: {resp.error?.code} - {resp.error?.info}");

            // 1 USD -> metal (alım gücü)
            if (!resp.rates.TryGetValue("XAU", out var xauPerUsd) ||
                !resp.rates.TryGetValue("XAG", out var xagPerUsd) ||
                !resp.rates.TryGetValue("XPT", out var xptPerUsd) ||
                !resp.rates.TryGetValue("XPD", out var xpdPerUsd))
            {
                throw new InvalidOperationException("metals-api.com response.rates missing XAU/XAG/XPT/XPD.");
            }

            // 1 metal -> USD (mevcut kolonların mantığı, ekrandaki gibi büyük değerler)
            if (!resp.rates.TryGetValue("USDXAU", out var usdPerXau) ||
                !resp.rates.TryGetValue("USDXAG", out var usdPerXag) ||
                !resp.rates.TryGetValue("USDXPT", out var usdPerXpt) ||
                !resp.rates.TryGetValue("USDXPD", out var usdPerXpd))
            {
                throw new InvalidOperationException("metals-api.com response.rates missing USDXAU/USDXAG/USDXPT/USDXPD.");
            }

            return new MetalPriceSnapshot
            {
                TakenAtUtc = DateTimeOffset.FromUnixTimeSeconds(resp.timestamp).UtcDateTime,
                RunSlot = slot,
                BaseCurrency = resp.@base ?? "USD",

                // Eski kolonlar (1 metal kaç USD)
                XAU = usdPerXau,
                XAG = usdPerXag,
                XPT = usdPerXpt,
                XPD = usdPerXpd,

                // Yeni kolonlar (1 USD kaç metal)
                XAU_PerUsd = xauPerUsd,
                XAG_PerUsd = xagPerUsd,
                XPT_PerUsd = xptPerUsd,
                XPD_PerUsd = xpdPerUsd,

                Source = "metals-api.com"
            };
        }


        private async Task UpsertServiceScheduleAsync(AppDbContext db, CancellationToken ct, string slot, IReadOnlyList<TimeOnly> times)
        {
            var nowUtc = DateTime.UtcNow;

            // Mevcut kolonlarına uyum için: ilk iki zamanı morning/evening flag gibi işaretle
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

            if (updated > 0) return;

            db.ServiceSchedules.Add(new ServiceSchedule
            {
                MorningTime = morningValue,
                EveningTime = eveningValue,
                UpdatedAtUtc = nowUtc
            });
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
            => ContainsSqlError(ex, 2601, 2627);

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
    }
}
