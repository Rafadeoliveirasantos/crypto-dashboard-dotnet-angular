using System.ComponentModel.DataAnnotations;

namespace CryptoDashboard.Dto.Crypto
{
    public class SettingsDto
    {
        [Range(60, 3600, ErrorMessage = "Intervalo deve estar entre 60 e 3600 segundos (1 min - 1 hora).")]
        public int UpdateIntervalSeconds { get; set; } = 300;

        [Required(ErrorMessage = "Moeda padrão é obrigatória.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Código da moeda deve ter exatamente 3 letras.")]
        [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Moeda deve ter 3 letras maiúsculas (ex: USD, BRL).")]
        public string DefaultCurrency { get; set; } = "USD";

        // 🆕 Propriedades adicionais
        [Range(1, 60, ErrorMessage = "Cache deve estar entre 1 e 60 minutos.")]
        public int CacheDurationMinutes { get; set; } = 2;

        [Range(5, 120, ErrorMessage = "Cache de backup deve estar entre 5 e 120 minutos.")]
        public int BackupCacheDurationMinutes { get; set; } = 30;

        public string Environment { get; set; } = "Development";

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public string? UpdatedBy { get; set; } = "System";
    }
}