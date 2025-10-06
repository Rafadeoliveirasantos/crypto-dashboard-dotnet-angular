namespace CryptoDashboard.Dto.Crypto
{
    public class CryptoDetailDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Symbol { get; set; }
        public string? Image { get; set; }
        public decimal MarketCap { get; set; }
        public decimal CurrentPrice { get; set; }
        public double CirculatingSupply { get; set; }
        public Dictionary<string, object> Links { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}