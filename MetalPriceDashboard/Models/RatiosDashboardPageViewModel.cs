namespace MetalPriceDashboard.Models;

public sealed class RatiosDashboardPageViewModel
{
    public DateTime? SnapshotTime { get; set; }
    public DateTime? CreatedAt { get; set; }

    public RatiosDashboardFilter Filter { get; set; } = new();

    public List<string> BaseMetals { get; set; } = new();
    public List<RatioCardViewModel> Cards { get; set; } = new();
}