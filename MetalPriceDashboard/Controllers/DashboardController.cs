using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalPriceDashboard.Data;
using MetalPriceDashboard.Models;

namespace MetalPriceDashboard.Controllers;

public sealed class DashboardController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(AppDbContext dbContext, ILogger<DashboardController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new DashboardIndexViewModel
        {
            MetalRecordCount = await _dbContext.MetalPricePeriodSummaries
                .AsNoTracking()
                .CountAsync(cancellationToken),

            RateRecordCount = await _dbContext.RatePeriodSummaries
                .AsNoTracking()
                .CountAsync(cancellationToken),

            RatioRecordCount = await _dbContext.MetalPriceRatios
                .AsNoTracking()
                .CountAsync(cancellationToken),
        };

        return View(model);
    }

    public async Task<IActionResult> Metal(
        [FromQuery] MetalDashboardFilter filter,
        CancellationToken cancellationToken)
    {
        IQueryable<MetalPricePeriodSummary> query = _dbContext.MetalPricePeriodSummaries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.BaseCurrency))
        {
            query = query.Where(x => x.BaseCurrency == filter.BaseCurrency);
        }

        if (!string.IsNullOrWhiteSpace(filter.Metal))
        {
            query = query.Where(x => x.Metal == filter.Metal);
        }

        if (!string.IsNullOrWhiteSpace(filter.PeriodType))
        {
            query = query.Where(x => x.PeriodType == filter.PeriodType);
        }

        var items = await query
            .OrderBy(x => x.BaseCurrency)
            .ThenBy(x => x.MetalSort)
            .ThenBy(x => x.PeriodStartDate)
            .ThenBy(x => x.PeriodTypeSort)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var model = new MetalDashboardPageViewModel
        {
            Filter = filter,
            Items = items
        };

        return View(model);
    }
    #region rates Process

    public async Task<IActionResult> Rates([FromQuery] RatesDashboardFilter filter, CancellationToken cancellationToken)
    {
        var currencyCodes = await _dbContext.RatePeriodSummaries
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.CurrencyCode))
            .Select(x => x.CurrencyCode)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        IQueryable<RatePeriodSummary> query = _dbContext.RatePeriodSummaries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.CurrencyCode))
        {
            query = query.Where(x => x.CurrencyCode == filter.CurrencyCode);
        }

        if (!string.IsNullOrWhiteSpace(filter.PeriodType))
        {
            query = query.Where(x => x.PeriodType == filter.PeriodType);
        }

        var items = await query
            .OrderBy(x => x.CurrencyCode)
            .ThenBy(x => x.PeriodStartDate)
            .ThenBy(x => x.PeriodTypeSort)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var model = new RatesDashboardPageViewModel
        {
            Filter = filter,
            Items = items,
            CurrencyCodes = currencyCodes
        };

        return View(model);
    }

    public async Task<IActionResult> RatesV2([FromQuery] RatesDashboardFilter filter, CancellationToken cancellationToken)
    {
        var currencyCodes = await _dbContext.RatePeriodSummaries
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.CurrencyCode))
            .Select(x => x.CurrencyCode)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        IQueryable<RatePeriodSummary> query = _dbContext.RatePeriodSummaries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.CurrencyCode))
        {
            query = query.Where(x => x.CurrencyCode == filter.CurrencyCode);
        }

        if (!string.IsNullOrWhiteSpace(filter.PeriodType))
        {
            query = query.Where(x => x.PeriodType == filter.PeriodType);
        }

        var items = await query
            .OrderBy(x => x.CurrencyCode)
            .ThenBy(x => x.PeriodStartDate)
            .ThenBy(x => x.PeriodTypeSort)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var model = new RatesDashboardPageViewModel
        {
            Filter = filter,
            Items = items,
            CurrencyCodes = currencyCodes
        };

        model.Cards = BuildRateCards(items, filter);

        return View(model);
    }

    private static List<RateCardViewModel> BuildRateCards(
    List<RatePeriodSummary> items,
    RatesDashboardFilter filter)
    {
        if (items == null || items.Count == 0)
        {
            return [];
        }

        var selectedCurrencies = !string.IsNullOrWhiteSpace(filter.CurrencyCode)
             ? new List<string> { filter.CurrencyCode! }
    :        items
            .Select(x => x.CurrencyCode)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var cards = new List<RateCardViewModel>();

        foreach (var currencyCode in selectedCurrencies)
        {
            var history = items
                .Where(x => string.Equals(x.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.PeriodStartDate)
                .ThenBy(x => x.PeriodTypeSort)
                .TakeLast(20)
                .ToList();

            if (history.Count == 0)
            {
                continue;
            }

            cards.Add(BuildRateCard(
                key: $"{currencyCode}_ForexBuying",
                title: $"{currencyCode} / Forex Buying",
                currencyCode: currencyCode,
                metricName: "Forex Buying",
                history: history,
                selector: x => ((x.ForexBuyingMin ?? 0m) + (x.ForexBuyingMax ?? 0m)) / 2m));

            cards.Add(BuildRateCard(
                key: $"{currencyCode}_ForexSelling",
                title: $"{currencyCode} / Forex Selling",
                currencyCode: currencyCode,
                metricName: "Forex Selling",
                history: history,
                selector: x => ((x.ForexSellingMin ?? 0m) + (x.ForexSellingMax ?? 0m)) / 2m));

            cards.Add(BuildRateCard(
                key: $"{currencyCode}_BanknoteSelling",
                title: $"{currencyCode} / Banknote Selling",
                currencyCode: currencyCode,
                metricName: "Banknote Selling",
                history: history,
                selector: x => ((x.BanknoteSellingMin ?? 0m) + (x.BanknoteSellingMax ?? 0m)) / 2m));
        }

        return cards;
    }

    private static RateCardViewModel BuildRateCard(
        string key,
        string title,
        string currencyCode,
        string metricName,
        List<RatePeriodSummary> history,
        Func<RatePeriodSummary, decimal> selector)
    {
        var values = history.Select(selector).ToList();
        var current = values.LastOrDefault();
        var previous = values.Count > 1 ? values[^2] : (decimal?)null;

        var change = previous.HasValue ? current - previous.Value : 0m;
        var changePercent = previous.HasValue && previous.Value != 0m
            ? (change / previous.Value) * 100m
            : 0m;

        var direction = "flat";
        var arrow = "→";

        if (previous.HasValue)
        {
            if (current > previous.Value)
            {
                direction = "up";
                arrow = "↑";
            }
            else if (current < previous.Value)
            {
                direction = "down";
                arrow = "↓";
            }
        }

        var average = values.Count == 0 ? 0m : values.Average();

        string valuation;
        if (average == 0m)
        {
            valuation = "Fair";
        }
        else if (current > average * 1.02m)
        {
            valuation = "Overvalued";
        }
        else if (current < average * 0.98m)
        {
            valuation = "Undervalued";
        }
        else
        {
            valuation = "Fair";
        }

        var interpretation = BuildRateInterpretation(currencyCode, metricName, direction, valuation);

        return new RateCardViewModel
        {
            Key = key,
            Title = title,
            CurrencyCode = currencyCode,
            MetricName = metricName,
            CurrentValue = current,
            PreviousValue = previous,
            Change = change,
            ChangePercent = changePercent,
            Average = average,
            Valuation = valuation,
            Direction = direction,
            DirectionArrow = arrow,
            Interpretation = interpretation,
            History = values,
            SparklinePoints = BuildSparklinePoints(values)
        };
    }

    private static string BuildRateInterpretation(
        string currencyCode,
        string metricName,
        string direction,
        string valuation)
    {
        return valuation switch
        {
            "Overvalued" => $"{currencyCode} için {metricName} kısa dönem ortalamasının üzerinde.",
            "Undervalued" => $"{currencyCode} için {metricName} kısa dönem ortalamasının altında.",
            _ => direction switch
            {
                "up" => $"{currencyCode} için {metricName} kısa vadede yükseliş eğiliminde.",
                "down" => $"{currencyCode} için {metricName} kısa vadede düşüş eğiliminde.",
                _ => $"{currencyCode} için {metricName} tarafında belirgin kısa vadeli yön yok."
            }
        };
    }


    #endregion
    #region Ratios Process
    public async Task<IActionResult> Ratios(CancellationToken cancellationToken)
    {
        var snapshots = await _dbContext.MetalPriceRatios
            .AsNoTracking()
            .OrderByDescending(x => x.SnapshotTime)
            .ThenByDescending(x => x.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        var latest = snapshots.FirstOrDefault();

        var model = new RatiosDashboardPageViewModel
        {
            SnapshotTime = latest?.SnapshotTime,
            CreatedAt = latest?.CreatedAt
        };

        if (latest is null)
        {
            return View(model);
        }

        var orderedHistory = snapshots
            .OrderBy(x => x.SnapshotTime)
            .ThenBy(x => x.Id)
            .ToList();

        model.Cards =
        [
            BuildRatioCard("XAU_XAG", "XAU / XAG", "XAU", "XAG", orderedHistory, x => x.XAU_XAG),
            BuildRatioCard("XAU_XPT", "XAU / XPT", "XAU", "XPT", orderedHistory, x => x.XAU_XPT),
            BuildRatioCard("XAU_XPD", "XAU / XPD", "XAU", "XPD", orderedHistory, x => x.XAU_XPD),

            BuildRatioCard("XAG_XAU", "XAG / XAU", "XAG", "XAU", orderedHistory, x => x.XAG_XAU),
            BuildRatioCard("XAG_XPT", "XAG / XPT", "XAG", "XPT", orderedHistory, x => x.XAG_XPT),
            BuildRatioCard("XAG_XPD", "XAG / XPD", "XAG", "XPD", orderedHistory, x => x.XAG_XPD),

            BuildRatioCard("XPT_XAU", "XPT / XAU", "XPT", "XAU", orderedHistory, x => x.XPT_XAU),
            BuildRatioCard("XPT_XAG", "XPT / XAG", "XPT", "XAG", orderedHistory, x => x.XPT_XAG),
            BuildRatioCard("XPT_XPD", "XPT / XPD", "XPT", "XPD", orderedHistory, x => x.XPT_XPD),

            BuildRatioCard("XPD_XAU", "XPD / XAU", "XPD", "XAU", orderedHistory, x => x.XPD_XAU),
            BuildRatioCard("XPD_XAG", "XPD / XAG", "XPD", "XAG", orderedHistory, x => x.XPD_XAG),
            BuildRatioCard("XPD_XPT", "XPD / XPT", "XPD", "XPT", orderedHistory, x => x.XPD_XPT)
        ];

        return View(model);
    }

    private static RatioCardViewModel BuildRatioCard(
        string key,
        string title,
        string baseMetal,
        string quoteMetal,
        List<MetalPriceRatio> history,
        Func<MetalPriceRatio, decimal> selector)
    {
        var values = history.Select(selector).ToList();
        var current = values.LastOrDefault();
        var previous = values.Count > 1 ? values[^2] : (decimal?)null;

        var change = previous.HasValue ? current - previous.Value : 0m;
        var changePercent = previous.HasValue && previous.Value != 0m
            ? (change / previous.Value) * 100m
            : 0m;

        var direction = "flat";
        var arrow = "→";

        if (previous.HasValue)
        {
            if (current > previous.Value)
            {
                direction = "up";
                arrow = "↑";
            }
            else if (current < previous.Value)
            {
                direction = "down";
                arrow = "↓";
            }
        }

        var average = values.Count == 0 ? 0m : values.Average();

        string valuation;
        if (average == 0m)
        {
            valuation = "Fair";
        }
        else if (current > average * 1.02m)
        {
            valuation = "Overvalued";
        }
        else if (current < average * 0.98m)
        {
            valuation = "Undervalued";
        }
        else
        {
            valuation = "Fair";
        }

        var interpretation = BuildInterpretation(baseMetal, quoteMetal, direction, valuation);

        return new RatioCardViewModel
        {
            Key = key,
            Title = title,
            BaseMetal = baseMetal,
            QuoteMetal = quoteMetal,
            CurrentValue = current,
            PreviousValue = previous,
            Change = change,
            ChangePercent = changePercent,
            Average = average,
            Valuation = valuation,
            Direction = direction,
            DirectionArrow = arrow,
            Interpretation = interpretation,
            History = values,
            SparklinePoints = BuildSparklinePoints(values)
        };
    }

    private static string BuildInterpretation(
       string baseMetal,
       string quoteMetal,
       string direction,
       string valuation)
    {
        return valuation switch
        {
            "Overvalued" => $"{baseMetal}, {quoteMetal}'ye göre kısa dönem ortalamasının üzerinde fiyatlanıyor.",
            "Undervalued" => $"{baseMetal}, {quoteMetal}'ye göre kısa dönem ortalamasının altında fiyatlanıyor.",
            _ => direction switch
            {
                "up" => $"{baseMetal}, {quoteMetal}'ye göre kısa vadede güçleniyor.",
                "down" => $"{baseMetal}, {quoteMetal}'ye göre kısa vadede zayıflıyor.",
                _ => $"{baseMetal} / {quoteMetal} için belirgin kısa vadeli yön yok."
            }
        };
    }

    private static string BuildSparklinePoints(IReadOnlyList<decimal> values, int width = 140, int height = 42)
    {
        if (values == null || values.Count == 0)
        {
            return string.Empty;
        }

        if (values.Count == 1)
        {
            var y = height / 2m;
            return $"0,{y:0.##} {width},{y:0.##}";
        }

        var min = values.Min();
        var max = values.Max();
        var range = max - min;

        if (range == 0m)
        {
            range = 1m;
        }

        var stepX = (decimal)width / (values.Count - 1);

        var points = values.Select((value, index) =>
        {
            var x = index * stepX;
            var normalized = (value - min) / range;
            var y = height - (normalized * height);
            return $"{x:0.##},{y:0.##}";
        });

        return string.Join(" ", points);
    }

    public async Task<IActionResult> RatiosV1(CancellationToken cancellationToken)
    {
        var latest = await _dbContext.MetalPriceRatios
            .AsNoTracking()
            .OrderByDescending(x => x.SnapshotTime)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var model = new RatiosDashboardViewModel
        {
            Latest = latest
        };

        return View(model);
    }
    #endregion

    public IActionResult Error()
    {
        return View();
    }
}