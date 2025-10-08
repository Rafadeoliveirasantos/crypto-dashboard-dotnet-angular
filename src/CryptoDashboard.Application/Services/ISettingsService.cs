using CryptoDashboard.Dto.Crypto;

namespace CryptoDashboard.Application.Services
{
    /// <summary>
    /// Interface para gerenciamento de configurações do sistema CryptoDashboard
    /// Autor: Rafadeoliveirasantos
    /// Data: 2025-10-08
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Obtém as configurações atuais do sistema
        /// </summary>
        /// <returns>DTO contendo todas as configurações (intervalo, moeda, cache, etc.)</returns>
        SettingsDto GetSettings();

        /// <summary>
        /// Atualiza as configurações do sistema
        /// </summary>
        /// <param name="dto">Novas configurações a serem aplicadas</param>
        /// <exception cref="ArgumentNullException">Quando dto é nulo</exception>
        /// <exception cref="ArgumentException">Quando valores são inválidos</exception>
        void UpdateSettings(SettingsDto dto);

        /// <summary>
        /// Reseta todas as configurações para os valores padrão do sistema
        /// </summary>
        void ResetToDefaults();
    }
}