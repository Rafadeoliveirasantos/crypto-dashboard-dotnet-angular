namespace CryptoDashboard.Dto.Crypto
{
    public class AlertHistoryDto
    {
        public Guid AlertId { get; set; }
        public string CryptoId { get; set; } = string.Empty;
        public decimal TriggeredPrice { get; set; }
        public DateTime TriggeredAt { get; set; }
    }
}
