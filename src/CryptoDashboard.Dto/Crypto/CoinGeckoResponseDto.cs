namespace CryptoDashboard.Dto.Crypto
{
    public class CoinGeckoResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal CurrentPriceBrl { get; set; }
        public decimal MarketCap { get; set; }
        public decimal PriceChangePercentage24h { get; set; }
        public decimal TotalVolume { get; set; }
        public DateTime LastUpdated { get; set; }
        public SparklineDto? SparklineIn7d { get; set; }
    }

    public class SparklineDto
    {
        public List<decimal> Price { get; set; } = new();
    }
}
