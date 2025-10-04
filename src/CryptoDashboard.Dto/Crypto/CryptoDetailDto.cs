namespace CryptoDashboard.Dto.Crypto
{
    public class CryptoDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public decimal MarketCap { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal CirculatingSupply { get; set; }
        public Dictionary<string, string> Links { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}