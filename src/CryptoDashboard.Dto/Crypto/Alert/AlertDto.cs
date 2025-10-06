namespace CryptoDashboard.Dto.Crypto.Alert
{
    public class AlertDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CryptoId { get; set; } = string.Empty;
        public decimal TargetPrice { get; set; }
        public bool IsMaxAlert { get; set; } // true = alerta máximo, false = mínimo
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
