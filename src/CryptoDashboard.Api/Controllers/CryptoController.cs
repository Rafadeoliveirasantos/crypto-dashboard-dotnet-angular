using CryptoDashboard.Application.Services;
using CryptoDashboard.Dto.Crypto;
using Microsoft.AspNetCore.Mvc;

namespace CryptoDashboard.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CryptoController : ControllerBase
    {
        private readonly ICryptoService _cryptoService;

        public CryptoController(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        // GET /cryptos?search=btc&minPrice=100&maxPrice=1000&variation=positive&orderBy=marketCap&direction=desc
        [HttpGet]
        public async Task<IActionResult> GetCryptos(
            [FromQuery] string? search = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? variation = null,
            [FromQuery] string? orderBy = null,
            [FromQuery] string? direction = "desc"
        )
        {
            var result = await _cryptoService.GetCryptosAsync(search, minPrice, maxPrice, variation, orderBy, direction);
            return Ok(result);
        }

        // GET /cryptos/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCryptoDetail(string id)
        {
            var detail = await _cryptoService.GetCryptoDetailAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        // GET /cryptos/{id}/chart?days=30
        [HttpGet("{id}/chart")]
        public async Task<IActionResult> GetPriceChart(string id, [FromQuery] int days = 30)
        {
            var chart = await _cryptoService.GetPriceChartAsync(id, days);
            if (chart == null)
                return NotFound();
            return Ok(chart);
        }

        // GET /cryptos/exchange-rates?baseCurrency=USD&symbols=BRL,EUR
        [HttpGet("exchange-rates")]
        public async Task<IActionResult> GetExchangeRates([FromQuery] string baseCurrency = "USD", [FromQuery] string symbols = "BRL,EUR")
        {
            var symbolArray = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var rates = await _cryptoService.GetExchangeRatesAsync(baseCurrency, symbolArray);
            return Ok(rates);
        }

        // POST /cryptos/{id}/favorite
        [HttpPost("{id}/favorite")]
        public async Task<IActionResult> AddFavorite(string id)
        {
            await _cryptoService.AddFavoriteAsync(id);
            return NoContent();
        }

        // DELETE /cryptos/{id}/favorite
        [HttpDelete("{id}/favorite")]
        public async Task<IActionResult> RemoveFavorite(string id)
        {
            await _cryptoService.RemoveFavoriteAsync(id);
            return NoContent();
        }

        // GET /cryptos/favorites
        [HttpGet("favorites")]
        public async Task<IActionResult> GetFavorites()
        {
            var favorites = await _cryptoService.GetFavoritesAsync();
            return Ok(favorites);
        }

        // POST /cryptos/{id}/alerts
        [HttpPost("{id}/alerts")]
        public async Task<IActionResult> AddAlert(string id, [FromBody] AlertDto dto)
        {
            dto.CryptoId = id;
            await _cryptoService.AddAlertAsync(dto);
            return NoContent();
        }

        // GET /cryptos/alerts
        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts()
        {
            var alerts = await _cryptoService.GetAlertsAsync();
            return Ok(alerts);
        }

        // DELETE /cryptos/alerts/{alertId}
        [HttpDelete("alerts/{alertId}")]
        public async Task<IActionResult> RemoveAlert(Guid alertId)
        {
            await _cryptoService.RemoveAlertAsync(alertId);
            return NoContent();
        }

        // GET /cryptos/alerts/history
        [HttpGet("alerts/history")]
        public async Task<IActionResult> GetAlertHistory()
        {
            var history = await _cryptoService.GetAlertHistoryAsync();
            return Ok(history);
        }
    }
}