namespace CoinMarketCap.Service.Shared.Options
{
    public class CoinGeckoOptions
    {
        public const string SectionName = "CoinGecko";

        //public string MarketsUrl { get; set; } =
        //    "https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=market_cap_desc&per_page=100&page=1";
        public string BaseUrl { get; set; } = "https://api.coingecko.com/";
        public string MarketsPath { get; set; } = "api/v3/coins/markets";
        public string VsCurrency { get; set; } = "usd";
        public string Order { get; set; } = "market_cap_desc";
        public int PerPage { get; set; } = 100;
        public int Page { get; set; } = 1;
    }
}