namespace MetalPriceDashboard.Models
{
    public class RatioCardViewModel
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string BaseMetal { get; set; } = string.Empty;
        public string QuoteMetal { get; set; } = string.Empty;

        public int SortOrder { get; set; }
        public string ValueLabel { get; set; } = "Current Ratio";

        public decimal CurrentValue { get; set; }
        public decimal? PreviousValue { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercent { get; set; }

        public decimal Average { get; set; }
        public string Valuation { get; set; } = "Fair";

        public string Direction { get; set; } = "flat";
        public string DirectionArrow { get; set; } = "→";

        public string Interpretation { get; set; } = string.Empty;
        public List<decimal> History { get; set; } = new();
        public string SparklinePoints { get; set; } = string.Empty;
    }
}
