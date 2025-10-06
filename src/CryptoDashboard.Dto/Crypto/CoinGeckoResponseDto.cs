using System.Text.Json.Serialization;

namespace CryptoDashboard.Dto
{
    public class CoinGeckoResponseDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("symbol")]
        public string? Symbol { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("image")]
        public ImageInfo? Image { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTime? LastUpdated { get; set; }

        [JsonPropertyName("market_data")]
        public MarketDataDto? MarketData { get; set; }
    }
}