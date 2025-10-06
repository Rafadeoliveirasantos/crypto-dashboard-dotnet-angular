using Microsoft.AspNetCore.Mvc;
using CryptoDashboard.Application.Services;
using CryptoDashboard.Infrastructure.Services;

namespace CryptoDashboard.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExportController : ControllerBase
    {
        private readonly ICryptoService _cryptoService;

        public ExportController(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        [HttpGet("cryptos")]
        public async Task<IActionResult> ExportCryptos([FromQuery] string type = "json")
        {
            var cryptos = await _cryptoService.GetCryptosAsync();
            if (type.ToLower() == "csv")
                return File(ExportHelper.ToCsv(cryptos), "text/csv", "cryptos.csv");
            return Ok(cryptos);
        }

        [HttpGet("favorites")]
        public async Task<IActionResult> ExportFavorites([FromQuery] string type = "json")
        {
            var favorites = await _cryptoService.GetFavoritesAsync();
            if (type.ToLower() == "csv")
                return File(ExportHelper.ToCsv(favorites), "text/csv", "favorites.csv");
            return Ok(favorites);
        }

        [HttpGet("alerts")]
        public async Task<IActionResult> ExportAlerts([FromQuery] string type = "json")
        {
            var alerts = await _cryptoService.GetAlertsAsync();
            if (type.ToLower() == "csv")
                return File(ExportHelper.ToCsv(alerts), "text/csv", "alerts.csv");
            return Ok(alerts);
        }
    }
}