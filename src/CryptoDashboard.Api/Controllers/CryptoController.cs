using CryptoDashboard.Application.Services;
using CryptoDashboard.Dto.Crypto.Alert;
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

        [HttpGet("compare")]
        public async Task<IActionResult> CompareCryptos([FromQuery] string ids)
        {
            var idArray = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (idArray.Length == 0 || idArray.Length > 3)
                return BadRequest("Você deve informar entre 1 e 3 ids de moedas para comparar.");

            var allCryptos = await _cryptoService.GetCryptosAsync();
            var selection = allCryptos.Where(c => idArray.Contains(c.Id)).ToList();

            // Se quiser retornar campos específicos, pode criar um DTO CompareCryptoDto
            return Ok(selection);
        }

        //Estatísticas 
        [HttpGet("stats/top-gainers")]
        public async Task<IActionResult> GetTopGainers([FromQuery] int count = 5)
        {
            var cryptos = await _cryptoService.GetCryptosAsync();
            var topGainers = cryptos
                .OrderByDescending(c => c.Variation24h)
                .Take(count)
                .ToList();
            return Ok(topGainers);
        }

        [HttpGet("stats/top-losers")]
        public async Task<IActionResult> GetTopLosers([FromQuery] int count = 5)
        {
            var cryptos = await _cryptoService.GetCryptosAsync();
            var topLosers = cryptos
                .OrderBy(c => c.Variation24h)
                .Take(count)
                .ToList();
            return Ok(topLosers);
        }

        //Converter 
        [HttpGet("convert")]
        public async Task<IActionResult> ConvertCurrency([FromQuery] string from, [FromQuery] string to, [FromQuery] decimal amount)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || amount <= 0)
                return BadRequest("Parâmetros inválidos.");

            var rates = await _cryptoService.GetExchangeRatesAsync(from, to);
            if (rates.Rates.TryGetValue(to, out var rate))
            {
                // Tenta converter Bid para decimal
                if (decimal.TryParse(rate.Bid, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bidValue))
                {
                    var converted = amount * bidValue;
                    return Ok(new
                    {
                        From = from,
                        To = to,
                        Amount = amount,
                        Rate = bidValue,
                        Converted = converted
                    });
                }
                else
                {
                    return BadRequest("Valor da cotação inválido.");
                }
            }
            return BadRequest($"Taxa de conversão {from}->{to} não encontrada.");
        }
    }
}