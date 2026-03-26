namespace MetalPriceDashboard.Models
{
    public class MetalPriceSnapshot
    {
        public long Id { get; set; }
        public DateTime TakenAtUtc { get; set; }

        public decimal XAU { get; set; }
        public decimal XAG { get; set; }
        public decimal XPT { get; set; }
        public decimal XPD { get; set; }
    }
}
