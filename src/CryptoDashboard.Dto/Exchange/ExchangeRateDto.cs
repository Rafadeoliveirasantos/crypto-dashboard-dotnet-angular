namespace CryptoDashboard.Dto
{
    public class ExchangeRateDto
    {
        public Dictionary<string, CurrencyRateDto> Rates { get; set; } = new();
    }

    public class CurrencyRateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Codein { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Bid { get; set; } = string.Empty;
        public string Ask { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string Create_date { get; set; } = string.Empty;
    }
}