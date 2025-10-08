using CryptoDashboard.Application.Services; // 🆕 Importa da Application
using CryptoDashboard.Dto.Crypto;
using CryptoDashboard.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace CryptoDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(ISettingsService settingsService, ILogger<SettingsController> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        // GET /api/settings
        [HttpGet]
        public IActionResult GetSettings()
        {
            try
            {
                _logger.LogInformation("⚙️ [GET /api/settings] Solicitado por: Rafadeoliveirasantos");
                var settings = _settingsService.GetSettings();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar configurações");
                return StatusCode(500, new { error = "Erro ao buscar configurações", message = ex.Message });
            }
        }

        // GET /api/settings/update-interval
        [HttpGet("update-interval")]
        public IActionResult GetUpdateInterval()
        {
            try
            {
                _logger.LogInformation("⏱️ [GET /api/settings/update-interval] Solicitado");
                var settings = _settingsService.GetSettings();
                var interval = settings.UpdateIntervalSeconds;
                _logger.LogInformation("✅ Intervalo retornado: {Interval}s", interval);
                return Ok(interval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar intervalo");
                return StatusCode(500, new { error = "Erro ao buscar intervalo", message = ex.Message });
            }
        }

        // POST /api/settings
        [HttpPost]
        public IActionResult UpdateSettings([FromBody] SettingsDto dto)
        {
            try
            {
                _logger.LogInformation("⚙️ [POST /api/settings] Atualização solicitada");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    _logger.LogWarning("⚠️ Dados inválidos: {Errors}", string.Join(", ", errors));
                    return BadRequest(new { error = "Dados inválidos", details = errors });
                }

                _settingsService.UpdateSettings(dto);
                _logger.LogInformation("✅ Configurações atualizadas com sucesso");
                return Ok(dto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "⚠️ Argumento inválido");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao atualizar configurações");
                return StatusCode(500, new { error = "Erro ao atualizar configurações", message = ex.Message });
            }
        }

        // DELETE /api/settings
        [HttpDelete]
        public IActionResult ResetSettings()
        {
            try
            {
                _logger.LogInformation("🔄 [DELETE /api/settings] Reset solicitado por: Rafadeoliveirasantos");
                _settingsService.ResetToDefaults();
                _logger.LogInformation("✅ Configurações resetadas");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao resetar configurações");
                return StatusCode(500, new { error = "Erro ao resetar configurações" });
            }
        }
    }
}