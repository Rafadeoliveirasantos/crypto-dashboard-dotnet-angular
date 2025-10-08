using System.Text.Json.Serialization;

namespace CryptoDashboard.Dto.Crypto
{
    public class PriceChartDto
    {
        [JsonPropertyName("prices")]
        public List<List<decimal>> Prices { get; set; } = new();

        [JsonPropertyName("market_caps")]
        public List<List<decimal>> MarketCaps { get; set; } = new();

        [JsonPropertyName("total_volumes")]
        public List<List<decimal>> TotalVolumes { get; set; } = new();

        // 🆕 Propriedades auxiliares
        [JsonIgnore]
        public bool HasData => Prices != null && Prices.Any();

        [JsonIgnore]
        public int DataPointsCount => Prices?.Count ?? 0;

        // 🆕 Métodos auxiliares para extrair dados
        [JsonIgnore]
        public List<DateTime> Timestamps =>
            Prices?.Select(p => DateTimeOffset.FromUnixTimeMilliseconds((long)p[0]).DateTime).ToList()
            ?? new List<DateTime>();

        [JsonIgnore]
        public List<decimal> PriceValues =>
            Prices?.Select(p => p.Count > 1 ? p[1] : 0).ToList()
            ?? new List<decimal>();

        [JsonIgnore]
        public decimal MinPrice => PriceValues.Any() ? PriceValues.Min() : 0;

        [JsonIgnore]
        public decimal MaxPrice => PriceValues.Any() ? PriceValues.Max() : 0;

        [JsonIgnore]
        public decimal AvgPrice => PriceValues.Any() ? PriceValues.Average() : 0;

        // 🆕 Método para obter dados formatados
        public List<ChartDataPoint> GetFormattedData()
        {
            return Prices?.Select(p => new ChartDataPoint
            {
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)p[0]).DateTime,
                Price = p.Count > 1 ? p[1] : 0
            }).ToList() ?? new List<ChartDataPoint>();
        }
    }

    // 🆕 Classe auxiliar para dados formatados
    public class ChartDataPoint
    {
        public DateTime Timestamp { get; set; }
        public decimal Price { get; set; }
    }
}