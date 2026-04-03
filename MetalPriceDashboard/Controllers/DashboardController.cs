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
            .ThenByDescending(x => x.PeriodStartDate)
            .ThenByDescending(x => x.PeriodEndDate)
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
        var priorityCurrencies = new[] { "USD", "EUR", "GBP" };

        var rawCurrencyCodes = await _dbContext.RatePeriodSummaries
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.CurrencyCode))
            .Select(x => x.CurrencyCode)
            .Distinct()
            .ToListAsync(cancellationToken);

        var currencyCodes = rawCurrencyCodes
            .OrderBy(x =>
            {
                var index = Array.FindIndex(
                    priorityCurrencies,
                    p => string.Equals(p, x, StringComparison.OrdinalIgnoreCase));

                return index == -1 ? int.MaxValue : index;
            })
            .ThenBy(x => x)
            .ToList();

        IQueryable<RatePeriodSummary> historyQuery = _dbContext.RatePeriodSummaries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.CurrencyCode))
        {
            historyQuery = historyQuery.Where(x => x.CurrencyCode == filter.CurrencyCode);
        }

        if (!string.IsNullOrWhiteSpace(filter.PeriodType))
        {
            historyQuery = historyQuery.Where(x => x.PeriodType == filter.PeriodType);
        }

        var historyItems = await historyQuery
            .OrderBy(x => x.CurrencyCode)
            .ThenBy(x => x.PeriodStartDate)
            .ThenBy(x => x.PeriodTypeSort)
            .ToListAsync(cancellationToken);

        IQueryable<RatePeriodSummary> currentReferenceQuery = _dbContext.RatePeriodSummaries
            .AsNoTracking()
            .Where(x => x.PeriodType == "MONTH");

        if (!string.IsNullOrWhiteSpace(filter.CurrencyCode))
        {
            currentReferenceQuery = currentReferenceQuery.Where(x => x.CurrencyCode == filter.CurrencyCode);
        }

        var currentReferenceItems = await currentReferenceQuery
            .OrderBy(x => x.CurrencyCode)
            .ThenByDescending(x => x.PeriodEndDate)
            .ThenByDescending(x => x.PeriodStartDate)
            .ToListAsync(cancellationToken);

        var latestCurrentByCurrency = currentReferenceItems
            .GroupBy(x => x.CurrencyCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.First(),
                StringComparer.OrdinalIgnoreCase);

        var model = new RatesDashboardPageViewModel
        {
            Filter = filter,
            Items = historyItems,
            CurrencyCodes = currencyCodes,
            Cards = BuildRateCards(historyItems, latestCurrentByCurrency, filter)
        };

        return View(model);
    }

    private static List<RateCardViewModel> BuildRateCards(
        List<RatePeriodSummary> historyItems,
        IReadOnlyDictionary<string, RatePeriodSummary> latestCurrentByCurrency,
        RatesDashboardFilter filter)
    {
        if (historyItems == null || historyItems.Count == 0)
        {
            return [];
        }

        var priorityCurrencies = new[] { "USD", "EUR", "GBP" };

        var selectedCurrencies = !string.IsNullOrWhiteSpace(filter.CurrencyCode)
            ? new List<string> { filter.CurrencyCode! }
            : historyItems
                .Select(x => x.CurrencyCode)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x =>
                {
                    var index = Array.FindIndex(
                        priorityCurrencies,
                        p => string.Equals(p, x, StringComparison.OrdinalIgnoreCase));

                    return index == -1 ? int.MaxValue : index;
                })
                .ThenBy(x => x)
                .ToList();

        var cards = new List<RateCardViewModel>();

        foreach (var currencyCode in selectedCurrencies)
        {
            var history = historyItems
                .Where(x => string.Equals(x.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.PeriodStartDate)
                .ThenBy(x => x.PeriodTypeSort)
                .TakeLast(20)
                .ToList();

            if (history.Count == 0)
            {
                continue;
            }

            latestCurrentByCurrency.TryGetValue(currencyCode, out var latestCurrent);

            cards.Add(BuildRateCard(
                key: $"{currencyCode}_ForexBuying",
                title: $"{currencyCode} / Forex Buying",
                currencyCode: currencyCode,
                metricName: "Forex Buying",
                selectedPeriodType: filter.PeriodType,
                history: history,
                latestCurrent: latestCurrent,
                selector: x => ((x.ForexBuyingMin ?? 0m) + (x.ForexBuyingMax ?? 0m)) / 2m));

            cards.Add(BuildRateCard(
                key: $"{currencyCode}_ForexSelling",
                title: $"{currencyCode} / Forex Selling",
                currencyCode: currencyCode,
                metricName: "Forex Selling",
                selectedPeriodType: filter.PeriodType,
                history: history,
                latestCurrent: latestCurrent,
                selector: x => ((x.ForexSellingMin ?? 0m) + (x.ForexSellingMax ?? 0m)) / 2m));

            cards.Add(BuildRateCard(
                key: $"{currencyCode}_BanknoteSelling",
                title: $"{currencyCode} / Banknote Selling",
                currencyCode: currencyCode,
                metricName: "Banknote Selling",
                selectedPeriodType: filter.PeriodType,
                history: history,
                latestCurrent: latestCurrent,
                selector: x => ((x.BanknoteSellingMin ?? 0m) + (x.BanknoteSellingMax ?? 0m)) / 2m));
        }

        return cards;
    }

    private static RateCardViewModel BuildRateCard(
        string key,
        string title,
        string currencyCode,
        string metricName,
        string? selectedPeriodType,
        List<RatePeriodSummary> history,
        RatePeriodSummary? latestCurrent,
        Func<RatePeriodSummary, decimal> selector)
    {
        var historyValues = history.Select(selector).ToList();
        var historyCurrent = historyValues.LastOrDefault();
        var historyPrevious = historyValues.Count > 1 ? historyValues[^2] : (decimal?)null;
        var historyAverage = historyValues.Count == 0 ? 0m : historyValues.Average();

        var currentValue = latestCurrent != null
            ? selector(latestCurrent)
            : historyCurrent;

        var benchmarkValue = historyAverage;

        var currentChange = benchmarkValue != 0m
            ? currentValue - benchmarkValue
            : 0m;

        var currentChangePercent = benchmarkValue != 0m
            ? (currentChange / benchmarkValue) * 100m
            : 0m;

        var direction = "flat";
        var arrow = "→";

        if (benchmarkValue != 0m)
        {
            if (currentValue > benchmarkValue)
            {
                direction = "up";
                arrow = "↑";
            }
            else if (currentValue < benchmarkValue)
            {
                direction = "down";
                arrow = "↓";
            }
        }

        string valuation;
        if (historyAverage == 0m)
        {
            valuation = "Fair";
        }
        else if (historyCurrent > historyAverage * 1.02m)
        {
            valuation = "Overvalued";
        }
        else if (historyCurrent < historyAverage * 0.98m)
        {
            valuation = "Undervalued";
        }
        else
        {
            valuation = "Fair";
        }

        var interpretation = BuildRateInterpretation(
            currencyCode: currencyCode,
            metricName: metricName,
            selectedPeriodType: selectedPeriodType,
            direction: direction,
            valuation: valuation);

        return new RateCardViewModel
        {
            Key = key,
            Title = title,
            CurrencyCode = currencyCode,
            MetricName = metricName,
            CurrentValue = currentValue,
            PreviousValue = historyPrevious,
            Change = currentChange,
            ChangePercent = currentChangePercent,
            Average = historyAverage,
            Valuation = valuation,
            Direction = direction,
            DirectionArrow = arrow,
            Interpretation = interpretation,
            History = historyValues,
            SparklinePoints = BuildSparklinePoints(historyValues)
        };
    }

    private static string BuildRateInterpretation(
        string currencyCode,
        string metricName,
        string? selectedPeriodType,
        string direction,
        string valuation)
    {
        var periodLabel = selectedPeriodType switch
        {
            "WEEK" => "haftalık",
            "MONTH" => "aylık",
            "3MONTH" => "3 aylık",
            "6MONTH" => "6 aylık",
            "YEAR" => "yıllık",
            "2YEAR" => "2 yıllık",
            "3YEAR" => "3 yıllık",
            "5YEAR" => "5 yıllık",
            _ => "seçili dönem"
        };

        var valuationText = valuation switch
        {
            "Overvalued" => $"{currencyCode} için {metricName}, {periodLabel} ortalamanın üzerinde seyrediyor.",
            "Undervalued" => $"{currencyCode} için {metricName}, {periodLabel} ortalamanın altında seyrediyor.",
            _ => $"{currencyCode} için {metricName}, {periodLabel} ortalamaya yakın seyrediyor."
        };

        var directionText = direction switch
        {
            "up" => "Güncel referans değer seçili dönem ortalamasının üzerinde.",
            "down" => "Güncel referans değer seçili dönem ortalamasının altında.",
            _ => "Güncel referans değer seçili dönem ortalamasına yakın."
        };

        return $"{valuationText} {directionText}";
    }
     
    #endregion

    #region Ratios Process

    public async Task<IActionResult> Ratios(CancellationToken cancellationToken)
    {
        var ratioSnapshots = await _dbContext.MetalPriceRatios
            .AsNoTracking()
            .OrderByDescending(x => x.SnapshotTime)
            .ThenByDescending(x => x.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        var latestRatio = ratioSnapshots.FirstOrDefault();

        var model = new RatiosDashboardPageViewModel
        {
            SnapshotTime = latestRatio?.SnapshotTime,
            CreatedAt = latestRatio?.CreatedAt
        };

        if (latestRatio is null)
        {
            return View(model);
        }

        var priceSnapshots = await _dbContext.MetalPriceSnapshots
            .AsNoTracking()
            .OrderByDescending(x => x.TakenAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        var orderedRatioHistory = ratioSnapshots
            .OrderBy(x => x.SnapshotTime)
            .ThenBy(x => x.Id)
            .ToList();

        var orderedPriceHistory = priceSnapshots
            .OrderBy(x => x.TakenAtUtc)
            .ThenBy(x => x.Id)
            .ToList();

        model.Cards =
        [
            BuildPriceCard("XAU_PRICE", "XAU / USD", "XAU", orderedPriceHistory, x => x.XAU),
            BuildRatioCard("XAU_XAG", "XAU / XAG", "XAU", "XAG", orderedRatioHistory, x => x.XAU_XAG, 1),
            BuildRatioCard("XAU_XPT", "XAU / XPT", "XAU", "XPT", orderedRatioHistory, x => x.XAU_XPT, 2),
            BuildRatioCard("XAU_XPD", "XAU / XPD", "XAU", "XPD", orderedRatioHistory, x => x.XAU_XPD, 3),

            BuildPriceCard("XAG_PRICE", "XAG / USD", "XAG", orderedPriceHistory, x => x.XAG),
            BuildRatioCard("XAG_XAU", "XAG / XAU", "XAG", "XAU", orderedRatioHistory, x => x.XAG_XAU, 1),
            BuildRatioCard("XAG_XPT", "XAG / XPT", "XAG", "XPT", orderedRatioHistory, x => x.XAG_XPT, 2),
            BuildRatioCard("XAG_XPD", "XAG / XPD", "XAG", "XPD", orderedRatioHistory, x => x.XAG_XPD, 3),

            BuildPriceCard("XPT_PRICE", "XPT / USD", "XPT", orderedPriceHistory, x => x.XPT),
            BuildRatioCard("XPT_XAU", "XPT / XAU", "XPT", "XAU", orderedRatioHistory, x => x.XPT_XAU, 1),
            BuildRatioCard("XPT_XAG", "XPT / XAG", "XPT", "XAG", orderedRatioHistory, x => x.XPT_XAG, 2),
            BuildRatioCard("XPT_XPD", "XPT / XPD", "XPT", "XPD", orderedRatioHistory, x => x.XPT_XPD, 3),

            BuildPriceCard("XPD_PRICE", "XPD / USD", "XPD", orderedPriceHistory, x => x.XPD),
            BuildRatioCard("XPD_XAU", "XPD / XAU", "XPD", "XAU", orderedRatioHistory, x => x.XPD_XAU, 1),
            BuildRatioCard("XPD_XAG", "XPD / XAG", "XPD", "XAG", orderedRatioHistory, x => x.XPD_XAG, 2),
            BuildRatioCard("XPD_XPT", "XPD / XPT", "XPD", "XPT", orderedRatioHistory, x => x.XPD_XPT, 3)
        ];

        return View(model);
    }

    private static RatioCardViewModel BuildRatioCard(
        string key,
        string title,
        string baseMetal,
        string quoteMetal,
        List<MetalPriceRatio> history,
        Func<MetalPriceRatio, decimal> selector,
        int sortOrder)
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

        var interpretation = BuildRatioInterpretation(baseMetal, quoteMetal, direction, valuation);

        return new RatioCardViewModel
        {
            Key = key,
            Title = title,
            BaseMetal = baseMetal,
            QuoteMetal = quoteMetal,
            SortOrder = sortOrder,
            ValueLabel = "Current Ratio",
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

    private static RatioCardViewModel BuildPriceCard(
        string key,
        string title,
        string baseMetal,
        List<MetalPriceSnapshot> history,
        Func<MetalPriceSnapshot, decimal> selector)
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

        var interpretation = BuildPriceInterpretation(baseMetal, direction, valuation);

        return new RatioCardViewModel
        {
            Key = key,
            Title = title,
            BaseMetal = baseMetal,
            QuoteMetal = "USD",
            SortOrder = 0,
            ValueLabel = "Current Price",
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

    private static string BuildPriceInterpretation(
        string metal,
        string direction,
        string valuation)
    {
        return valuation switch
        {
            "Overvalued" => $"{metal} fiyatı kısa dönem ortalamasının üzerinde.",
            "Undervalued" => $"{metal} fiyatı kısa dönem ortalamasının altında.",
            _ => direction switch
            {
                "up" => $"{metal} fiyatı kısa vadede yükseliş eğiliminde.",
                "down" => $"{metal} fiyatı kısa vadede düşüş eğiliminde.",
                _ => $"{metal} fiyatında belirgin kısa vadeli yön yok."
            }
        };
    }

    private static string BuildRatioInterpretation(
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

        var latestSnapshot = await _dbContext.MetalPriceSnapshots
            .AsNoTracking()
            .OrderByDescending(x => x.TakenAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var model = new RatiosDashboardViewModel
        {
            Latest = latest,
            LatestSnapshot = latestSnapshot
        };

        return View(model);
    }

    #endregion

    public IActionResult Error()
    {
        return View();
    }
}