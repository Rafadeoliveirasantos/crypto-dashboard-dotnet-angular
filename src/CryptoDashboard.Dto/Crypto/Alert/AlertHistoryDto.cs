namespace CryptoDashboard.Dto.Crypto.Alert
{
    public class AlertHistoryDto
    {
        // --- PROPRIEDADES ADICIONADAS PARA UM HISTÓRICO COMPLETO ---

        /// <summary>
        /// O ID da criptomoeda (ex: "bitcoin").
        /// </summary>
        public string CryptoId { get; set; } = string.Empty;

        /// <summary>
        /// O nome da criptomoeda (ex: "Bitcoin") para fácil exibição.
        /// </summary>
        public string? CryptoName { get; set; }

        /// <summary>
        /// O tipo de alerta que foi disparado ("min" ou "max").
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// O preço-alvo que estava configurado no alerta.
        /// </summary>
        public decimal TargetPrice { get; set; }

        /// <summary>
        /// O preço exato da moeda no momento em que o alerta foi disparado.
        /// </summary>
        public decimal TriggeredPrice { get; set; }

        /// <summary>
        /// A data e hora (UTC) em que o alerta foi disparado.
        /// </summary>
        public DateTime TriggeredAt { get; set; }
    }
}