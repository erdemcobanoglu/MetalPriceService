namespace MetalPriceDashboard.Models
{
    public class RateCardViewModel
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        public string CurrencyCode { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;

        public decimal CurrentValue { get; set; }
        public decimal? PreviousValue { get; set; }

        public decimal Change { get; set; }
        public decimal ChangePercent { get; set; }

        public decimal Average { get; set; }
        public string Valuation { get; set; } = "Fair";

        public string Direction { get; set; } = "flat";
        public string DirectionArrow { get; set; } = "→";

        public string Interpretation { get; set; } = string.Empty;

        public List<decimal> History { get; set; } = [];
        public string SparklinePoints { get; set; } = string.Empty;
    }
}
