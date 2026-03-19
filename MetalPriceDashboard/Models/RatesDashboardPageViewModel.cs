namespace MetalPriceDashboard.Models
{
    public class RatesDashboardPageViewModel
    {
        public RatesDashboardFilter Filter { get; set; } = new();
        public List<RatePeriodSummary> Items { get; set; } = [];
        public List<string> CurrencyCodes { get; set; } = [];
        public List<RateCardViewModel> Cards { get; set; } = [];
    }
}
