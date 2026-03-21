using System.Text.Json.Serialization;


namespace CoinMarketCap.Service.Infrastructure.Http.Dtos
{
    public sealed class CoinMarketCapLatestQuotesResponseDto
    {
        [JsonPropertyName("data")]
        public Dictionary<string, CoinMarketCapCoinDto> Data { get; set; } = new();
    }

    public sealed class CoinMarketCapCoinDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = default!;

        [JsonPropertyName("quote")]
        public Dictionary<string, CoinMarketCapQuoteDto> Quote { get; set; } = new();
    }

    public sealed class CoinMarketCapQuoteDto
    {
        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("market_cap")]
        public decimal? MarketCap { get; set; }

        [JsonPropertyName("volume_24h")]
        public decimal? Volume24h { get; set; }

        [JsonPropertyName("percent_change_1h")]
        public decimal? PercentChange1h { get; set; }

        [JsonPropertyName("percent_change_24h")]
        public decimal? PercentChange24h { get; set; }

        [JsonPropertyName("percent_change_7d")]
        public decimal? PercentChange7d { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}
