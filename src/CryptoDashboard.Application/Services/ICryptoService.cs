using CryptoDashboard.Domain;
using CryptoDashboard.Dto;
using CryptoDashboard.Dto.Crypto;

namespace CryptoDashboard.Application.Services
{
    public interface ICryptoService
    {
        // Dados principais
        Task<List<CryptoCurrency>> GetCryptosAsync(
            string? search = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? variation = null,
            string? orderBy = null,
            string? direction = "desc"
        );
        Task<CryptoDetailDto> GetCryptoDetailAsync(string id);
        Task<PriceChartDto> GetPriceChartAsync(string id, int days);
        Task<ExchangeRateDto> GetExchangeRatesAsync(string baseCurrency, params string[] symbols);

        // Favoritos
        Task AddFavoriteAsync(string cryptoId);
        Task RemoveFavoriteAsync(string cryptoId);
        Task<List<CryptoCurrency>> GetFavoritesAsync();

        //Alert
        Task AddAlertAsync(AlertDto alert);
        Task RemoveAlertAsync(Guid alertId);
        Task<List<AlertDto>> GetAlertsAsync();
        Task<List<AlertHistoryDto>> GetAlertHistoryAsync();
    }
}