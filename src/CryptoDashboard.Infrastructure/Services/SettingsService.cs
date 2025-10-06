using CryptoDashboard.Dto.Crypto;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoDashboard.Infrastructure.Services
{
    public interface ISettingsService
    {
        SettingsDto GetSettings();
        void UpdateSettings(SettingsDto dto);
    }

    public class SettingsService : ISettingsService
    {
        private const string SettingsKey = "SystemSettings";
        private readonly IMemoryCache _cache;

        public SettingsService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public SettingsDto GetSettings()
        {
            return _cache.Get<SettingsDto>(SettingsKey) ?? new SettingsDto();
        }

        public void UpdateSettings(SettingsDto dto)
        {
            _cache.Set(SettingsKey, dto);
        }
    }
}