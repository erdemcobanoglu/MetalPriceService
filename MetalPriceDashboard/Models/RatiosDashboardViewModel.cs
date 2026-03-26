using Microsoft.EntityFrameworkCore;

namespace MetalPriceDashboard.Models
{
    public class RatiosDashboardViewModel
    {
        public MetalPriceRatio? Latest { get; set; }
        public MetalPriceSnapshot? LatestSnapshot { get; set; }
    }
}
