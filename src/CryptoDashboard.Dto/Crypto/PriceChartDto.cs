namespace CryptoDashboard.Dto.Crypto
{
    public class PriceChartDto
    {
        public List<List<decimal>> prices { get; set; } = new();
        public List<List<decimal>> market_caps { get; set; } = new();
        public List<List<decimal>> total_volumes { get; set; } = new();
    }
}