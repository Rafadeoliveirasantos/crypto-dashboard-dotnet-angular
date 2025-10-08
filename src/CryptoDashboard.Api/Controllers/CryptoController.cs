using CryptoDashboard.Application.Services;
using CryptoDashboard.Dto.Crypto.Alert;
using Microsoft.AspNetCore.Mvc;

namespace CryptoDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/cryptos")]
    public class CryptoController : ControllerBase
    {
        private readonly ICryptoService _cryptoService;
        private readonly ILogger<CryptoController> _logger;

        public CryptoController(ICryptoService cryptoService, ILogger<CryptoController> logger)
        {
            _cryptoService = cryptoService;
            _logger = logger;
        }

        // GET /api/cryptos
        [HttpGet]
        public async Task<IActionResult> GetCryptos(
            [FromQuery] string? search = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? variation = null,
            [FromQuery] string? orderBy = null,
            [FromQuery] string? direction = "desc")
        {
            try
            {
                _logger.LogInformation("📊 Buscando criptomoedas (search: {Search}, minPrice: {MinPrice}, maxPrice: {MaxPrice})",
                    search, minPrice, maxPrice);

                var result = await _cryptoService.GetCryptosAsync(search, minPrice, maxPrice, variation, orderBy, direction);

                _logger.LogInformation("✅ {Count} criptomoedas retornadas", result.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar criptomoedas");
                return StatusCode(500, new { error = "Erro ao buscar criptomoedas", message = ex.Message });
            }
        }

        // GET /api/cryptos/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCryptoDetail(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("⚠️ ID de criptomoeda vazio ou nulo");
                    return BadRequest(new { error = "ID da criptomoeda é obrigatório" });
                }

                _logger.LogInformation("🔍 Buscando detalhes de: {Id}", id);

                var detail = await _cryptoService.GetCryptoDetailAsync(id);

                if (detail == null || string.IsNullOrEmpty(detail.Id))
                {
                    _logger.LogWarning("⚠️ Criptomoeda não encontrada: {Id}", id);
                    return NotFound(new { error = $"Criptomoeda '{id}' não encontrada" });
                }

                _logger.LogInformation("✅ Detalhes de {Name} ({Id}) retornados", detail.Name, id);
                return Ok(detail);
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("⚠️ Rate limit atingido ao buscar {Id}", id);
                return StatusCode(429, new { error = "Muitas requisições. Tente novamente em alguns segundos." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar detalhes de {Id}", id);
                return StatusCode(500, new { error = "Erro ao buscar detalhes da criptomoeda", message = ex.Message });
            }
        }

        // GET /api/cryptos/{id}/chart?days=7
        [HttpGet("{id}/chart")]
        public async Task<IActionResult> GetPriceChart(string id, [FromQuery] int days = 7)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("⚠️ ID de criptomoeda vazio");
                    return BadRequest(new { error = "ID da criptomoeda é obrigatório" });
                }

                // 🆕 Validação de dias
                if (days < 1 || days > 365)
                {
                    _logger.LogWarning("⚠️ Dias inválido: {Days}", days);
                    return BadRequest(new { error = "Dias deve estar entre 1 e 365" });
                }

                _logger.LogInformation("📈 Buscando gráfico de {Id} ({Days} dias)", id, days);

                var chart = await _cryptoService.GetPriceChartAsync(id, days);

                if (chart == null || chart.Prices == null || !chart.Prices.Any())
                {
                    _logger.LogWarning("⚠️ Gráfico vazio para {Id}", id);
                    return NotFound(new { error = $"Dados de gráfico não encontrados para '{id}'" });
                }

                _logger.LogInformation("✅ Gráfico de {Id} retornado com {Count} pontos", id, chart.Prices.Count);
                return Ok(chart);
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("⚠️ Rate limit atingido ao buscar gráfico de {Id}", id);
                return StatusCode(429, new { error = "Muitas requisições. Tente novamente em alguns segundos." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar gráfico de {Id}", id);
                return StatusCode(500, new { error = "Erro ao buscar gráfico", message = ex.Message });
            }
        }

        // GET /api/cryptos/exchange-rates?baseCurrency=USD&symbols=BRL,EUR
        [HttpGet("exchange-rates")]
        public async Task<IActionResult> GetExchangeRates(
            [FromQuery] string baseCurrency = "USD",
            [FromQuery] string symbols = "BRL,EUR")
        {
            try
            {
                var symbolArray = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (symbolArray.Length == 0)
                {
                    _logger.LogWarning("⚠️ Nenhum símbolo fornecido");
                    return BadRequest(new { error = "Pelo menos um símbolo deve ser fornecido" });
                }

                _logger.LogInformation("💱 Buscando taxas de câmbio: {Base} → {Symbols}", baseCurrency, string.Join(", ", symbolArray));

                var rates = await _cryptoService.GetExchangeRatesAsync(baseCurrency, symbolArray);

                _logger.LogInformation("✅ Taxas de câmbio retornadas");
                return Ok(rates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar taxas de câmbio");
                return StatusCode(500, new { error = "Erro ao buscar taxas de câmbio", message = ex.Message });
            }
        }

        // POST /api/cryptos/{id}/favorite
        [HttpPost("{id}/favorite")]
        public async Task<IActionResult> AddFavorite(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { error = "ID da criptomoeda é obrigatório" });
                }

                _logger.LogInformation("⭐ Adicionando {Id} aos favoritos", id);

                await _cryptoService.AddFavoriteAsync(id);

                _logger.LogInformation("✅ {Id} adicionado aos favoritos", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao adicionar favorito: {Id}", id);
                return StatusCode(500, new { error = "Erro ao adicionar favorito" });
            }
        }

        // DELETE /api/cryptos/{id}/favorite
        [HttpDelete("{id}/favorite")]
        public async Task<IActionResult> RemoveFavorite(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { error = "ID da criptomoeda é obrigatório" });
                }

                _logger.LogInformation("⭐ Removendo {Id} dos favoritos", id);

                await _cryptoService.RemoveFavoriteAsync(id);

                _logger.LogInformation("✅ {Id} removido dos favoritos", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao remover favorito: {Id}", id);
                return StatusCode(500, new { error = "Erro ao remover favorito" });
            }
        }

        // GET /api/cryptos/favorites
        [HttpGet("favorites")]
        public async Task<IActionResult> GetFavorites()
        {
            try
            {
                _logger.LogInformation("⭐ Buscando favoritos");

                var favorites = await _cryptoService.GetFavoritesAsync();

                _logger.LogInformation("✅ {Count} favoritos retornados", favorites.Count);
                return Ok(favorites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar favoritos");
                return StatusCode(500, new { error = "Erro ao buscar favoritos" });
            }
        }

        // POST /api/cryptos/{id}/alerts
        [HttpPost("{id}/alerts")]
        public async Task<IActionResult> AddAlert(string id, [FromBody] AlertDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { error = "ID da criptomoeda é obrigatório" });
                }

                if (dto == null || dto.TargetPrice <= 0)
                {
                    return BadRequest(new { error = "Dados do alerta inválidos" });
                }

                dto.CryptoId = id;

                _logger.LogInformation("🔔 Criando alerta para {Id} - Preço alvo: {Price}", id, dto.TargetPrice);

                await _cryptoService.AddAlertAsync(dto);

                _logger.LogInformation("✅ Alerta criado para {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao criar alerta para {Id}", id);
                return StatusCode(500, new { error = "Erro ao criar alerta" });
            }
        }

        // GET /api/cryptos/alerts
        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts()
        {
            try
            {
                _logger.LogInformation("🔔 Buscando alertas");

                var alerts = await _cryptoService.GetAlertsAsync();

                _logger.LogInformation("✅ {Count} alertas retornados", alerts.Count);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar alertas");
                return StatusCode(500, new { error = "Erro ao buscar alertas" });
            }
        }

        // DELETE /api/cryptos/alerts/{alertId}
        [HttpDelete("alerts/{alertId}")]
        public async Task<IActionResult> RemoveAlert(Guid alertId)
        {
            try
            {
                _logger.LogInformation("🔔 Removendo alerta: {AlertId}", alertId);

                await _cryptoService.RemoveAlertAsync(alertId);

                _logger.LogInformation("✅ Alerta {AlertId} removido", alertId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao remover alerta: {AlertId}", alertId);
                return StatusCode(500, new { error = "Erro ao remover alerta" });
            }
        }

        // GET /api/cryptos/alerts/history
        [HttpGet("alerts/history")]
        public async Task<IActionResult> GetAlertHistory()
        {
            try
            {
                _logger.LogInformation("📜 Buscando histórico de alertas");

                var history = await _cryptoService.GetAlertHistoryAsync();

                _logger.LogInformation("✅ {Count} alertas no histórico", history.Count);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar histórico de alertas");
                return StatusCode(500, new { error = "Erro ao buscar histórico" });
            }
        }

        // GET /api/cryptos/compare?ids=bitcoin,ethereum,cardano
        [HttpGet("compare")]
        public async Task<IActionResult> CompareCryptos([FromQuery] string ids)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ids))
                {
                    return BadRequest(new { error = "IDs das criptomoedas são obrigatórios" });
                }

                var idArray = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (idArray.Length == 0 || idArray.Length > 3)
                {
                    _logger.LogWarning("⚠️ Quantidade inválida de IDs para comparação: {Count}", idArray.Length);
                    return BadRequest(new { error = "Você deve informar entre 1 e 3 IDs de moedas para comparar" });
                }

                _logger.LogInformation("🔄 Comparando criptomoedas: {Ids}", string.Join(", ", idArray));

                var allCryptos = await _cryptoService.GetCryptosAsync();
                var selection = allCryptos.Where(c => idArray.Contains(c.Id)).ToList();

                if (selection.Count == 0)
                {
                    _logger.LogWarning("⚠️ Nenhuma criptomoeda encontrada para comparação");
                    return NotFound(new { error = "Nenhuma criptomoeda encontrada com os IDs fornecidos" });
                }

                _logger.LogInformation("✅ {Count} criptomoedas retornadas para comparação", selection.Count);
                return Ok(selection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao comparar criptomoedas");
                return StatusCode(500, new { error = "Erro ao comparar criptomoedas" });
            }
        }

        // GET /api/cryptos/stats/top-gainers?count=5
        [HttpGet("stats/top-gainers")]
        public async Task<IActionResult> GetTopGainers([FromQuery] int count = 5)
        {
            try
            {
                if (count < 1 || count > 50)
                {
                    return BadRequest(new { error = "Count deve estar entre 1 e 50" });
                }

                _logger.LogInformation("📈 Buscando top {Count} gainers", count);

                var cryptos = await _cryptoService.GetCryptosAsync();
                var topGainers = cryptos
                    .OrderByDescending(c => c.Variation24h)
                    .Take(count)
                    .ToList();

                _logger.LogInformation("✅ {Count} top gainers retornados", topGainers.Count);
                return Ok(topGainers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar top gainers");
                return StatusCode(500, new { error = "Erro ao buscar top gainers" });
            }
        }

        // GET /api/cryptos/stats/top-losers?count=5
        [HttpGet("stats/top-losers")]
        public async Task<IActionResult> GetTopLosers([FromQuery] int count = 5)
        {
            try
            {
                if (count < 1 || count > 50)
                {
                    return BadRequest(new { error = "Count deve estar entre 1 e 50" });
                }

                _logger.LogInformation("📉 Buscando top {Count} losers", count);

                var cryptos = await _cryptoService.GetCryptosAsync();
                var topLosers = cryptos
                    .OrderBy(c => c.Variation24h)
                    .Take(count)
                    .ToList();

                _logger.LogInformation("✅ {Count} top losers retornados", topLosers.Count);
                return Ok(topLosers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar top losers");
                return StatusCode(500, new { error = "Erro ao buscar top losers" });
            }
        }

        // GET /api/cryptos/convert?from=bitcoin&to=usd&amount=1
        [HttpGet("convert")]
        public async Task<IActionResult> ConvertCurrency(
            [FromQuery] string from,
            [FromQuery] string to,
            [FromQuery] decimal amount)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                {
                    return BadRequest(new { error = "Parâmetros 'from' e 'to' são obrigatórios" });
                }

                if (amount <= 0)
                {
                    return BadRequest(new { error = "Amount deve ser maior que zero" });
                }

                _logger.LogInformation("💱 Convertendo {Amount} {From} para {To}", amount, from, to);

                var exchangeData = await _cryptoService.GetExchangeRatesAsync(to, from);

                if (exchangeData?.Rates != null &&
                    exchangeData.Rates.TryGetValue(from, out var currencyRates) &&
                    currencyRates.TryGetValue(to, out var rateValue))
                {
                    var convertedAmount = amount * rateValue;

                    _logger.LogInformation("✅ Conversão realizada: {Amount} {From} = {Converted} {To}",
                        amount, from, convertedAmount, to);

                    return Ok(new
                    {
                        From = from,
                        To = to,
                        Amount = amount,
                        Rate = rateValue,
                        ConvertedAmount = convertedAmount,
                        Timestamp = DateTime.UtcNow
                    });
                }

                _logger.LogWarning("⚠️ Cotação não encontrada: {From} → {To}", from, to);
                return NotFound(new { error = $"Cotação para converter de '{from}' para '{to}' não encontrada" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao converter moeda: {From} → {To}", from, to);
                return StatusCode(500, new { error = "Erro ao converter moeda" });
            }
        }
    }
}