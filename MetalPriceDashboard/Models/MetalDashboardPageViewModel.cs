namespace MetalPriceDashboard.Models
{
    public class MetalDashboardPageViewModel
    {
        public MetalDashboardFilter Filter { get; set; } = new();
        public IReadOnlyList<MetalPricePeriodSummary> Items { get; set; } = [];
        public List<string> BaseCurrencies { get; set; } = new();
    }
}
