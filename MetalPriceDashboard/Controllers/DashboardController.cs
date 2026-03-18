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
                .CountAsync(cancellationToken)
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

    public async Task<IActionResult> Rates(
        [FromQuery] RatesDashboardFilter filter,
        CancellationToken cancellationToken)
    {
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
            Items = items
        };

        return View(model);
    }

    public IActionResult Error()
    {
        return View();
    }
}