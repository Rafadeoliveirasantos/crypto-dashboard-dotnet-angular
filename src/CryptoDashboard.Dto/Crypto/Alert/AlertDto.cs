using System;

namespace CryptoDashboard.Dto.Crypto.Alert
{
    public class AlertDto
    {
        /// <summary>
        /// Identificador único para o alerta.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// O ID da criptomoeda (ex: "bitcoin").
        /// </summary>
        public string CryptoId { get; set; } = string.Empty;

        /// <summary>
        /// O preço-alvo a ser monitorado.
        /// </summary>
        public decimal TargetPrice { get; set; }

        /// <summary>
        /// O tipo do alerta: "min" (dispara se o preço for <= alvo)
        /// ou "max" (dispara se o preço for >= alvo).
        /// </summary>
        public string Type { get; set; } = string.Empty; // <-- PROPRIEDADE ADICIONADA
    }
}