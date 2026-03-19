namespace MetalPriceDashboard.Models
{
    public class RatiosDashboardPageViewModel
    {
        public DateTime? SnapshotTime { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<RatioCardViewModel> Cards { get; set; } = new();
    }
}
