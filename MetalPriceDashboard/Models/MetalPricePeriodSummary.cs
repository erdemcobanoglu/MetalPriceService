namespace MetalPriceDashboard.Models
{
    public class MetalPricePeriodSummary
    {
        public string BaseCurrency { get; set; } = default!;
        public string Metal { get; set; } = default!;
        public int MetalSort { get; set; }
        public string PeriodType { get; set; } = default!;
        public int PeriodTypeSort { get; set; }
        public int PeriodYear { get; set; }
        public int? PeriodMonth { get; set; }
        public long? WeekOfMonth { get; set; }
        public string PeriodLabel { get; set; } = default!;
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
    }
}
