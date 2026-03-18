namespace MetalPriceDashboard.Models
{
    public class RatePeriodSummary
    {
        public string CurrencyCode { get; set; } = default!;
        public int Unit { get; set; }
        public string PeriodType { get; set; } = default!;
        public int PeriodTypeSort { get; set; }
        public int PeriodYear { get; set; }
        public int? PeriodMonth { get; set; }
        public long? WeekOfMonth { get; set; }
        public string PeriodLabel { get; set; } = default!;
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }

        public decimal? ForexBuyingMin { get; set; }
        public decimal? ForexBuyingMax { get; set; }
        public decimal? ForexSellingMin { get; set; }
        public decimal? ForexSellingMax { get; set; }
        public decimal? BanknoteBuyingMin { get; set; }
        public decimal? BanknoteBuyingMax { get; set; }
        public decimal? BanknoteSellingMin { get; set; }
        public decimal? BanknoteSellingMax { get; set; }
    }
}
