using System.Text.Json.Serialization;

public sealed class CoinGeckoMarketItem
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("current_price")]
    public decimal? CurrentPrice { get; set; }

    [JsonPropertyName("market_cap")]
    public decimal? MarketCap { get; set; }

    [JsonPropertyName("total_volume")]
    public decimal? TotalVolume { get; set; }

    [JsonPropertyName("price_change_percentage_24h")]
    public decimal? PriceChangePercentage24h { get; set; }

    [JsonPropertyName("price_change_percentage_1h_in_currency")]
    public decimal? PriceChangePercentage1hInCurrency { get; set; }

    [JsonPropertyName("price_change_percentage_7d_in_currency")]
    public decimal? PriceChangePercentage7dInCurrency { get; set; }

    [JsonPropertyName("last_updated")]
    public DateTimeOffset? LastUpdated { get; set; }

    [JsonPropertyName("market_cap_rank")]
    public int? MarketCapRank { get; set; }
}