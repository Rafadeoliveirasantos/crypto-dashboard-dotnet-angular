namespace CryptoDashboard.Dto.Crypto
{
    public class CryptoStatsDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal PriceUsd { get; set; }
        public decimal Variation24h { get; set; }
    }
}