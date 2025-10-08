namespace CryptoDashboard.Domain.Entities
{
    public class CryptoCurrency
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal PriceUsd { get; set; }
        public decimal PriceBrl { get; set; }
        public decimal Variation24h { get; set; }
        public decimal MarketCap { get; set; }
        public decimal Volume24h { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsFavorite { get; set; }
    }
}