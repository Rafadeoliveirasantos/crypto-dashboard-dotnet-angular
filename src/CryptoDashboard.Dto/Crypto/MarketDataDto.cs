using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace CryptoDashboard.Dto
{
    public class MarketDataDto
    {
        [JsonPropertyName("current_price")]
        public Dictionary<string, decimal>? CurrentPrice { get; set; }

        [JsonPropertyName("market_cap")]
        public Dictionary<string, decimal>? MarketCap { get; set; }

        [JsonPropertyName("circulating_supply")]
        public double? CirculatingSupply { get; set; }
    }
}