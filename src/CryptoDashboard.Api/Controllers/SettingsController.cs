using Microsoft.AspNetCore.Mvc;
using CryptoDashboard.Dto.Crypto;
using CryptoDashboard.Infrastructure.Services;

namespace CryptoDashboard.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet]
        public IActionResult GetSettings()
        {
            var settings = _settingsService.GetSettings();
            return Ok(settings);
        }

        [HttpPost]
        public IActionResult UpdateSettings([FromBody] SettingsDto dto)
        {
            _settingsService.UpdateSettings(dto);
            return Ok(dto);
        }
    }
}