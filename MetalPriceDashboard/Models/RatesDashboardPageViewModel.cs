namespace MetalPriceDashboard.Models
{
    public class RatesDashboardPageViewModel
    {
        public RatesDashboardFilter Filter { get; set; } = new();
        public IReadOnlyList<RatePeriodSummary> Items { get; set; } = [];
    }
}
