namespace MetalPriceDashboard.Models
{
    public class MetalPriceRatio
    {
        public int Id { get; set; }
        public DateTime SnapshotTime { get; set; }

        public decimal XAU_XAG { get; set; }
        public decimal XAU_XPT { get; set; }
        public decimal XAU_XPD { get; set; }

        public decimal XAG_XAU { get; set; }
        public decimal XAG_XPT { get; set; }
        public decimal XAG_XPD { get; set; }

        public decimal XPT_XAU { get; set; }
        public decimal XPT_XAG { get; set; }
        public decimal XPT_XPD { get; set; }

        public decimal XPD_XAU { get; set; }
        public decimal XPD_XAG { get; set; }
        public decimal XPD_XPT { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
