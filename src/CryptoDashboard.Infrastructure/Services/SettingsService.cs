using CryptoDashboard.Application.Services;
using CryptoDashboard.Dto.Crypto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CryptoDashboard.Infrastructure.Services
{
    /// <summary>
    /// Implementação do serviço de configurações usando MemoryCache
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private const string SettingsKey = "SystemSettings";
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<SettingsService> logger)
        {
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        public SettingsDto GetSettings()
        {
            // Tenta buscar do cache
            if (_cache.TryGetValue(SettingsKey, out SettingsDto? cachedSettings) && cachedSettings != null)
            {
                _logger.LogDebug("📦 Configurações retornadas do cache");
                return cachedSettings;
            }

            // Se não tem no cache, carrega do appsettings.json
            _logger.LogInformation("📖 Carregando configurações do appsettings.json");

            var settings = new SettingsDto
            {
                UpdateIntervalSeconds = _configuration.GetValue<int>("AppSettings:UpdateIntervalSeconds", 300),
                DefaultCurrency = _configuration.GetValue<string>("AppSettings:DefaultCurrency", "USD")?.ToUpper() ?? "USD",
                CacheDurationMinutes = _configuration.GetValue<int>("AppSettings:CacheDurationMinutes", 2),
                BackupCacheDurationMinutes = _configuration.GetValue<int>("AppSettings:BackupCacheDurationMinutes", 30),
                Environment = _configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT", "Development") ?? "Development",
                LastUpdated = DateTime.UtcNow,
                UpdatedBy = "System"
            };

            // Validação
            if (settings.UpdateIntervalSeconds < 60)
            {
                _logger.LogWarning("⚠️ Intervalo muito baixo ({Interval}s), ajustando para 300s", settings.UpdateIntervalSeconds);
                settings.UpdateIntervalSeconds = 300;
            }

            // Salva no cache com expiração de 1 hora
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .SetPriority(CacheItemPriority.High);

            _cache.Set(SettingsKey, settings, cacheOptions);

            _logger.LogInformation("✅ Configurações carregadas: UpdateInterval={Interval}s, Currency={Currency}",
                settings.UpdateIntervalSeconds, settings.DefaultCurrency);

            return settings;
        }

        public void UpdateSettings(SettingsDto dto)
        {
            _logger.LogInformation("💾 Atualizando configurações");

            // Validação
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto), "Configurações não podem ser nulas");
            }

            if (dto.UpdateIntervalSeconds < 60 || dto.UpdateIntervalSeconds > 3600)
            {
                _logger.LogWarning("⚠️ Intervalo inválido: {Interval}s", dto.UpdateIntervalSeconds);
                throw new ArgumentException("Intervalo deve estar entre 60 e 3600 segundos (1 min - 1 hora)");
            }

            if (string.IsNullOrWhiteSpace(dto.DefaultCurrency) || dto.DefaultCurrency.Length != 3)
            {
                _logger.LogWarning("⚠️ Moeda inválida: {Currency}", dto.DefaultCurrency);
                throw new ArgumentException("Moeda deve ter exatamente 3 letras (ex: USD, BRL)");
            }

            // Normaliza moeda para maiúsculas
            dto.DefaultCurrency = dto.DefaultCurrency.ToUpper();

            // Atualiza metadados
            dto.LastUpdated = DateTime.UtcNow;
            dto.UpdatedBy = "Rafadeoliveirasantos"; // 🎯 Seu usuário!

            // Salva no cache
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .SetPriority(CacheItemPriority.High);

            _cache.Set(SettingsKey, dto, cacheOptions);

            _logger.LogInformation("✅ Configurações atualizadas: UpdateInterval={Interval}s, Currency={Currency}, UpdatedBy={User}",
                dto.UpdateIntervalSeconds, dto.DefaultCurrency, dto.UpdatedBy);
        }

        public void ResetToDefaults()
        {
            _logger.LogInformation("🔄 Resetando configurações para os valores padrão");

            var defaultSettings = new SettingsDto
            {
                UpdateIntervalSeconds = 300,
                DefaultCurrency = "USD",
                CacheDurationMinutes = 2,
                BackupCacheDurationMinutes = 30,
                Environment = _configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT", "Development") ?? "Development",
                LastUpdated = DateTime.UtcNow,
                UpdatedBy = "Rafadeoliveirasantos"
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .SetPriority(CacheItemPriority.High);

            _cache.Set(SettingsKey, defaultSettings, cacheOptions);

            _logger.LogInformation("✅ Configurações resetadas para o padrão");
        }
    }
}