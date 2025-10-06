using System.ComponentModel.DataAnnotations;

namespace CryptoDashboard.Dto.Crypto
{
    public class SettingsDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Intervalo deve ser maior que zero.")]
        public int UpdateIntervalSeconds { get; set; } = 60;

        [Required(ErrorMessage = "Moeda padrão obrigatória.")]
        [StringLength(3, ErrorMessage = "Código da moeda deve ter 3 letras.")]
        public string DefaultCurrency { get; set; } = "USD";
    }
}