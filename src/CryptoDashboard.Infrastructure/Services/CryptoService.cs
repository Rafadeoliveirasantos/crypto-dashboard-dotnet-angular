using CryptoDashboard.Application.Services;
using CryptoDashboard.Domain.Entities;
using CryptoDashboard.Dto;
using CryptoDashboard.Dto.Crypto;
using CryptoDashboard.Dto.Crypto.Alert;
using CryptoDashboard.Dto.Crypto.Exchange;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CryptoDashboard.Infrastructure.Services
{
    public class CryptoService : ICryptoService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<CryptoService> _logger;
        private readonly IMemoryCache _cache;
        private const string FavoritesKey = "UserFavorites";
        private static readonly List<AlertDto> _alerts = new();
        private static readonly List<AlertHistoryDto> _alertHistory = new();

        public CryptoService(IHttpClientFactory httpFactory, ILogger<CryptoService> logger, IMemoryCache cache)
        {
            _httpFactory = httpFactory;
            _logger = logger;
            _cache = cache;
        }

        public async Task<List<CryptoCurrency>> GetCryptosAsync(
            string? search = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? variation = null,
            string? orderBy = null,
            string? direction = "desc"
        )
        {
            const string cacheKey = "cryptos_usd_brl_top10";

            if (!_cache.TryGetValue(cacheKey, out List<CryptoCurrency>? cryptos))
            {
                _logger.LogInformation("Cache de criptomoedas não encontrado. Buscando dados da API.");
                var client = _httpFactory.CreateClient("CoinGecko");

                // --- URLs relativas, pois o BaseAddress já está configurado ---
                var urlUsd = "coins/markets?vs_currency=usd&order=market_cap_desc&per_page=10&page=1&sparkline=true&price_change_percentage=7d";
                var responseUsd = await client.GetAsync(urlUsd);
                responseUsd.EnsureSuccessStatusCode();
                var jsonUsd = await responseUsd.Content.ReadAsStringAsync();
                var listUsd = JsonSerializer.Deserialize<List<CoinGeckoMarketDto>>(jsonUsd) ?? new();

                var urlBrl = "coins/markets?vs_currency=brl&order=market_cap_desc&per_page=10&page=1&sparkline=false";
                var responseBrl = await client.GetAsync(urlBrl);
                responseBrl.EnsureSuccessStatusCode();
                var jsonBrl = await responseBrl.Content.ReadAsStringAsync();
                var listBrl = JsonSerializer.Deserialize<List<CoinGeckoMarketDto>>(jsonBrl) ?? new();

                var favoritesSet = GetFavoriteIds();

                // --- Mapeamento corrigido para tratar valores nulos ---
                cryptos = listUsd.Select(dto =>
                {
                    var brlDto = listBrl.FirstOrDefault(b => b.Id == dto.Id);
                    return new CryptoCurrency
                    {
                        Id = dto.Id ?? string.Empty,
                        Name = dto.Name ?? string.Empty,
                        Symbol = dto.Symbol ?? string.Empty,
                        ImageUrl = dto.Image ?? string.Empty,
                        PriceUsd = dto.CurrentPrice ?? 0,
                        MarketCap = dto.MarketCap ?? 0,
                        PriceBrl = brlDto?.CurrentPrice ?? 0,
                        Variation24h = dto.PriceChangePercentage24h ?? 0,
                        Volume24h = dto.TotalVolume ?? 0,
                        LastUpdated = dto.LastUpdated ?? DateTime.MinValue,
                        Trend7d = dto.SparklineIn7d?.Price ?? new List<decimal>(),
                        IsFavorite = favoritesSet.Contains(dto.Id ?? string.Empty)
                    };
                }).ToList();

                _cache.Set(cacheKey, cryptos, TimeSpan.FromMinutes(2));
                _logger.LogInformation("Cache de criptomoedas atualizado com dados da API.");
            }

            if (cryptos is null)
            {
                return new List<CryptoCurrency>();
            }

            IEnumerable<CryptoCurrency> query = cryptos;

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    (c.Name is not null && c.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (c.Symbol is not null && c.Symbol.Contains(search, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (minPrice.HasValue)
            {
                query = query.Where(c => c.PriceUsd >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(c => c.PriceUsd <= maxPrice.Value);
            }

            if (!string.IsNullOrWhiteSpace(variation))
            {
                if (variation.Equals("positive", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(c => c.Variation24h > 0);
                }
                else if (variation.Equals("negative", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(c => c.Variation24h < 0);
                }
            }

            bool desc = direction?.ToLower() != "asc";
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                query = orderBy.ToLower() switch
                {
                    "marketcap" => desc ? query.OrderByDescending(c => c.MarketCap) : query.OrderBy(c => c.MarketCap),
                    "price" => desc ? query.OrderByDescending(c => c.PriceUsd) : query.OrderBy(c => c.PriceUsd),
                    "volume" => desc ? query.OrderByDescending(c => c.Volume24h) : query.OrderBy(c => c.Volume24h),
                    "variation" => desc ? query.OrderByDescending(c => c.Variation24h) : query.OrderBy(c => c.Variation24h),
                    "name" => desc ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
                    _ => desc ? query.OrderByDescending(c => c.MarketCap) : query.OrderBy(c => c.MarketCap),
                };
            }

            return query.ToList();
        }

        public async Task<CryptoDetailDto> GetCryptoDetailAsync(string id)
        {
            var client = _httpFactory.CreateClient("CoinGecko");
            var url = $"coins/{id}?localization=false&tickers=false&market_data=true&community_data=false&developer_data=false&sparkline=false";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            var coinGeckoDto = JsonSerializer.Deserialize<CoinGeckoResponseDto>(json);

            if (coinGeckoDto?.MarketData == null)
                return new CryptoDetailDto();

            // --- CORREÇÃO APLICADA AQUI ---

            // Inicializa as variáveis com um valor padrão
            decimal currentPriceUsd = 0;
            decimal marketCapUsd = 0;

            // Usa o operador ?. para chamar TryGetValue apenas se o dicionário não for nulo
            coinGeckoDto.MarketData.CurrentPrice?.TryGetValue("usd", out currentPriceUsd);
            coinGeckoDto.MarketData.MarketCap?.TryGetValue("usd", out marketCapUsd);

            // Mapeamento corrigido e seguro para ler de `MarketData`
            return new CryptoDetailDto
            {
                Id = coinGeckoDto.Id,
                Name = coinGeckoDto.Name,
                Symbol = coinGeckoDto.Symbol,
                Image = coinGeckoDto.Image?.Large,
                MarketCap = marketCapUsd,
                CurrentPrice = currentPriceUsd,
                CirculatingSupply = coinGeckoDto.MarketData.CirculatingSupply ?? 0,
                Links = new Dictionary<string, object>(), // Preencha corretamente se necessário!
                LastUpdated = coinGeckoDto.LastUpdated ?? DateTime.MinValue
            };
        }

        public async Task<PriceChartDto> GetPriceChartAsync(string id, int days)
        {
            var client = _httpFactory.CreateClient("CoinGecko");
            var url = $"coins/{id}/market_chart?vs_currency=usd&days={days}";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PriceChartDto>(json) ?? new PriceChartDto();
        }

        public Task<ExchangeRateDto> GetExchangeRatesAsync(string baseCurrency, params string[] symbols)
        {
            return Task.FromResult(new ExchangeRateDto());
        }

        public Task AddFavoriteAsync(string cryptoId)
        {
            var favorites = GetFavoriteIds();
            if (favorites.Add(cryptoId))
            {
                _cache.Set(FavoritesKey, favorites);
            }
            return Task.CompletedTask;
        }

        public Task RemoveFavoriteAsync(string cryptoId)
        {
            var favorites = GetFavoriteIds();
            if (favorites.Remove(cryptoId))
            {
                _cache.Set(FavoritesKey, favorites);
            }
            return Task.CompletedTask;
        }

        public async Task<List<CryptoCurrency>> GetFavoritesAsync()
        {
            // Pega os dados do cache ou da API, já com o status de favorito atualizado
            var allCryptos = await GetCryptosAsync();
            return allCryptos.Where(c => c.IsFavorite).ToList();
        }

        private HashSet<string> GetFavoriteIds()
        {
            return _cache.Get<HashSet<string>>(FavoritesKey) ?? new HashSet<string>();
        }

        public Task AddAlertAsync(AlertDto alert)
        {
            _alerts.Add(alert);
            _logger.LogInformation("Alerta adicionado para {CryptoId} com alvo {TargetPrice}.", alert.CryptoId, alert.TargetPrice);
            return Task.CompletedTask;
        }

        public Task RemoveAlertAsync(Guid alertId)
        {
            var removedCount = _alerts.RemoveAll(a => a.Id == alertId);
            if (removedCount > 0) _logger.LogInformation("Alerta {AlertId} removido.", alertId);
            return Task.CompletedTask;
        }

        public Task<List<AlertDto>> GetAlertsAsync()
        {
            return Task.FromResult(_alerts.ToList());
        }

        public Task<List<AlertHistoryDto>> GetAlertHistoryAsync()
        {
            return Task.FromResult(_alertHistory.ToList());
        }

        public async Task ProcessAlertsAsync()
        {
            var cryptos = await GetCryptosAsync();
            if (!cryptos.Any() || !_alerts.Any())
            {
                return;
            }

            var triggeredAlerts = new List<AlertDto>();

            foreach (var alert in _alerts.ToList())
            {
                var crypto = cryptos.FirstOrDefault(c => c.Id == alert.CryptoId);
                if (crypto == null) continue;

                bool triggered = (alert.Type.Equals("max", StringComparison.OrdinalIgnoreCase) && crypto.PriceUsd >= alert.TargetPrice) ||
                                 (alert.Type.Equals("min", StringComparison.OrdinalIgnoreCase) && crypto.PriceUsd <= alert.TargetPrice);

                if (triggered)
                {
                    _logger.LogWarning("ALERTA DISPARADO: {CryptoName} atingiu o alvo de {TargetPrice}. Preço atual: {CurrentPrice}", crypto.Name, alert.TargetPrice, crypto.PriceUsd);

                    _alertHistory.Add(new AlertHistoryDto
                    {
                        CryptoId = alert.CryptoId,
                        CryptoName = crypto.Name,
                        TriggeredAt = DateTime.UtcNow,
                        TriggeredPrice = crypto.PriceUsd,
                        TargetPrice = alert.TargetPrice,
                        Type = alert.Type
                    });

                    triggeredAlerts.Add(alert);
                }
            }

            if (triggeredAlerts.Any())
            {
                _alerts.RemoveAll(a => triggeredAlerts.Contains(a));
                _logger.LogInformation("{TriggeredCount} alertas foram removidos da lista de ativos.", triggeredAlerts.Count);
            }
        }
    }
}