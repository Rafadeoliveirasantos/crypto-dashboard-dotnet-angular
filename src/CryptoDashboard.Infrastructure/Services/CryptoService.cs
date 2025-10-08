using CryptoDashboard.Application.Services;
using CryptoDashboard.Domain.Entities;
using CryptoDashboard.Dto;
using CryptoDashboard.Dto.Crypto;
using CryptoDashboard.Dto.Crypto.Alert;
using CryptoDashboard.Dto.Crypto.Exchange;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace CryptoDashboard.Infrastructure.Services
{
    public class CryptoService : ICryptoService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<CryptoService> _logger;
        private readonly IMemoryCache _cache;

        // 🆕 Constantes
        private const string FavoritesKey = "UserFavorites";
        private const string CacheKeyCryptos = "cryptos_usd_brl_top10";
        private const string CacheKeyBackup = "cryptos_backup"; // Cache de emergência

        // 🆕 Rate Limiting
        private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
        private static DateTime _lastApiCall = DateTime.MinValue;
        private static readonly TimeSpan MinTimeBetweenRequests = TimeSpan.FromSeconds(3);

        // Alertas (em memória)
        private static readonly List<AlertDto> _alerts = new();
        private static readonly List<AlertHistoryDto> _alertHistory = new();

        public CryptoService(
            IHttpClientFactory httpFactory,
            ILogger<CryptoService> logger,
            IMemoryCache cache)
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
            string? direction = "desc")
        {
            // 🆕 1. TENTA BUSCAR DO CACHE PRINCIPAL
            if (_cache.TryGetValue(CacheKeyCryptos, out List<CryptoCurrency>? cachedCryptos))
            {
                _logger.LogInformation("✅ Dados carregados do cache ({Count} cryptos)", cachedCryptos?.Count ?? 0);
                return ApplyFilters(cachedCryptos ?? new(), search, minPrice, maxPrice, variation, orderBy, direction);
            }

            // 🆕 2. CACHE MISS - BUSCAR DA API COM RATE LIMITING
            _logger.LogInformation("⚠️ Cache expirado. Buscando da API CoinGecko...");

            List<CryptoCurrency>? cryptos = null;

            try
            {
                cryptos = await FetchFromApiWithRateLimiting();

                if (cryptos != null && cryptos.Any())
                {
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(2))
                        .SetSize(1); // 🆕 ADICIONAR TAMANHO

                    var backupOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                        .SetSize(1); // 🆕 ADICIONAR TAMANHO

                    _logger.LogInformation("✅ {Count} criptomoedas carregadas e salvas em cache", cryptos.Count);
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("⚠️ Rate limit atingido (429). Tentando usar cache de backup...");
                cryptos = GetBackupCache();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar dados da API. Tentando usar cache de backup...");
                cryptos = GetBackupCache();
            }

            // 🆕 3. SE NÃO TEM DADOS, RETORNA LISTA VAZIA
            cryptos ??= new List<CryptoCurrency>();

            return ApplyFilters(cryptos, search, minPrice, maxPrice, variation, orderBy, direction);
        }

        // 🆕 MÉTODO PRIVADO: Busca da API com Rate Limiting
        private async Task<List<CryptoCurrency>?> FetchFromApiWithRateLimiting()
        {
            await _rateLimiter.WaitAsync();
            try
            {
                // 🆕 Respeita o intervalo mínimo entre requisições
                var timeSinceLastCall = DateTime.UtcNow - _lastApiCall;
                if (timeSinceLastCall < MinTimeBetweenRequests)
                {
                    var delay = MinTimeBetweenRequests - timeSinceLastCall;
                    _logger.LogInformation("⏱️ Aguardando {Seconds}s para respeitar rate limit...", delay.TotalSeconds);
                    await Task.Delay(delay);
                }

                var client = _httpFactory.CreateClient("CoinGecko");

                // 🆕 Requisição USD
                var urlUsd = "coins/markets?vs_currency=usd&order=market_cap_desc&per_page=10&page=1&sparkline=true&price_change_percentage=7d";
                var responseUsd = await client.GetAsync(urlUsd);

                // 🆕 Verifica se é 429
                if (responseUsd.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("⚠️ Rate limit detectado ao buscar USD");
                    throw new HttpRequestException("Too Many Requests", null, HttpStatusCode.TooManyRequests);
                }

                responseUsd.EnsureSuccessStatusCode();
                var jsonUsd = await responseUsd.Content.ReadAsStringAsync();
                var listUsd = JsonSerializer.Deserialize<List<CoinGeckoMarketDto>>(jsonUsd) ?? new();

                // 🆕 Pequeno delay entre requisições
                await Task.Delay(1000);

                // 🆕 Requisição BRL
                var urlBrl = "coins/markets?vs_currency=brl&order=market_cap_desc&per_page=10&page=1&sparkline=false";
                var responseBrl = await client.GetAsync(urlBrl);

                if (responseBrl.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("⚠️ Rate limit detectado ao buscar BRL");
                    throw new HttpRequestException("Too Many Requests", null, HttpStatusCode.TooManyRequests);
                }

                responseBrl.EnsureSuccessStatusCode();
                var jsonBrl = await responseBrl.Content.ReadAsStringAsync();
                var listBrl = JsonSerializer.Deserialize<List<CoinGeckoMarketDto>>(jsonBrl) ?? new();

                _lastApiCall = DateTime.UtcNow;

                // 🆕 Mapear dados
                var favoritesSet = GetFavoriteIds();
                var cryptos = listUsd.Select(dto =>
                {
                    var brlDto = listBrl.FirstOrDefault(b => b.Id == dto.Id);
                    return new CryptoCurrency
                    {
                        Id = dto.Id ?? string.Empty,
                        Name = dto.Name ?? string.Empty,
                        Symbol = dto.Symbol?.ToUpper() ?? string.Empty,
                        ImageUrl = dto.Image ?? string.Empty, // ✅ CORRETO
                        PriceUsd = dto.CurrentPrice ?? 0,
                        MarketCap = dto.MarketCap ?? 0,
                        PriceBrl = brlDto?.CurrentPrice ?? 0,
                        Variation24h = dto.PriceChangePercentage24h ?? 0,
                        Volume24h = dto.TotalVolume ?? 0,
                        LastUpdated = dto.LastUpdated ?? DateTime.UtcNow,
                        IsFavorite = favoritesSet.Contains(dto.Id ?? string.Empty)
                    };
                }).ToList();

                return cryptos;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        // 🆕 MÉTODO PRIVADO: Busca cache de backup
        private List<CryptoCurrency>? GetBackupCache()
        {
            if (_cache.TryGetValue(CacheKeyBackup, out List<CryptoCurrency>? backupData))
            {
                _logger.LogInformation("✅ Usando dados do cache de backup ({Count} cryptos)", backupData?.Count ?? 0);
                return backupData;
            }

            _logger.LogWarning("⚠️ Nenhum dado em cache disponível");
            return null;
        }

        // 🆕 MÉTODO PRIVADO: Aplicar filtros
        private List<CryptoCurrency> ApplyFilters(
            List<CryptoCurrency> cryptos,
            string? search,
            decimal? minPrice,
            decimal? maxPrice,
            string? variation,
            string? orderBy,
            string? direction)
        {
            IEnumerable<CryptoCurrency> query = cryptos;

            // Busca
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.Symbol.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            // Preço mínimo/máximo
            if (minPrice.HasValue)
                query = query.Where(c => c.PriceUsd >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(c => c.PriceUsd <= maxPrice.Value);

            // Variação
            if (!string.IsNullOrWhiteSpace(variation))
            {
                query = variation.ToLower() switch
                {
                    "positive" => query.Where(c => c.Variation24h > 0),
                    "negative" => query.Where(c => c.Variation24h < 0),
                    _ => query
                };
            }

            // Ordenação
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
                    _ => desc ? query.OrderByDescending(c => c.MarketCap) : query.OrderBy(c => c.MarketCap)
                };
            }

            return query.ToList();
        }

        // ===== DETALHES DA CRYPTO =====
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

            decimal currentPriceUsd = 0;
            decimal marketCapUsd = 0;

            coinGeckoDto.MarketData.CurrentPrice?.TryGetValue("usd", out currentPriceUsd);
            coinGeckoDto.MarketData.MarketCap?.TryGetValue("usd", out marketCapUsd);

            return new CryptoDetailDto
            {
                Id = coinGeckoDto.Id,
                Name = coinGeckoDto.Name,
                Symbol = coinGeckoDto.Symbol,
                Image = coinGeckoDto.Image?.Large,
                MarketCap = marketCapUsd,
                CurrentPrice = currentPriceUsd,
                CirculatingSupply = coinGeckoDto.MarketData.CirculatingSupply ?? 0,
                Links = new Dictionary<string, object>(),
                LastUpdated = coinGeckoDto.LastUpdated ?? DateTime.MinValue
            };
        }

        // ===== GRÁFICO DE PREÇOS - VERSÃO OTIMIZADA E SEM WARNINGS =====
        public async Task<PriceChartDto> GetPriceChartAsync(string id, int days)
        {
            var cacheKey = $"chart_{id}_{days}";

            // 🆕 1. Tenta buscar do cache primeiro
            if (_cache.TryGetValue(cacheKey, out PriceChartDto? cachedChart) && cachedChart != null)
            {
                _logger.LogInformation("✅ Gráfico de {Id} ({Days}d) carregado do cache", id, days);
                return cachedChart;
            }

            // 🆕 2. Cache miss - busca da API com rate limiting
            _logger.LogInformation("⚠️ Cache miss - Buscando gráfico de {Id} ({Days}d) da API", id, days);

            try
            {
                // 🆕 3. Aplica rate limiting
                await _rateLimiter.WaitAsync();
                try
                {
                    var timeSinceLastCall = DateTime.UtcNow - _lastApiCall;
                    if (timeSinceLastCall < MinTimeBetweenRequests)
                    {
                        var delay = MinTimeBetweenRequests - timeSinceLastCall;
                        _logger.LogInformation("⏱️ Aguardando {Seconds:F1}s para buscar gráfico...", delay.TotalSeconds);
                        await Task.Delay(delay);
                    }

                    var client = _httpFactory.CreateClient("CoinGecko");
                    var url = $"coins/{id}/market_chart?vs_currency=usd&days={days}";

                    var response = await client.GetAsync(url);

                    // 🆕 4. Verifica se é rate limit (429)
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning("⚠️ Rate limit (429) ao buscar gráfico de {Id}", id);

                        // Tenta retornar do cache de backup se existir
                        var backupKey = $"chart_backup_{id}";
                        if (_cache.TryGetValue(backupKey, out PriceChartDto? backupChart) && backupChart != null)
                        {
                            _logger.LogInformation("✅ Usando gráfico de backup para {Id}", id);
                            return backupChart;
                        }

                        // Se não tem backup, retorna vazio
                        _logger.LogWarning("⚠️ Sem cache de backup disponível para {Id}", id);
                        return new PriceChartDto();
                    }

                    response.EnsureSuccessStatusCode();
                    _lastApiCall = DateTime.UtcNow;

                    var json = await response.Content.ReadAsStringAsync();
                    var chart = JsonSerializer.Deserialize<PriceChartDto>(json) ?? new PriceChartDto();

                    // 🆕 5. Salva no cache principal (10 minutos)
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                    _cache.Set(cacheKey, chart, cacheOptions);

                    // 🆕 6. Salva backup (1 hora)
                    var backupCacheKey = $"chart_backup_{id}";
                    var backupOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromHours(1));
                    _cache.Set(backupCacheKey, chart, backupOptions);

                    _logger.LogInformation("✅ Gráfico de {Id} ({Days}d) carregado da API e salvo em cache ({Points} pontos)",
                        id, days, chart.DataPointsCount);

                    return chart;
                }
                finally
                {
                    _rateLimiter.Release();
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("⚠️ Rate limit atingido ao buscar gráfico de {Id}", id);

                // Tenta retornar cache de backup
                var backupKey = $"chart_backup_{id}";
                if (_cache.TryGetValue(backupKey, out PriceChartDto? backupChart) && backupChart != null)
                {
                    _logger.LogInformation("✅ Usando cache de backup para {Id}", id);
                    return backupChart;
                }

                _logger.LogWarning("⚠️ Sem cache de backup disponível para {Id}", id);
                return new PriceChartDto();
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("⏱️ Timeout ao buscar gráfico de {Id}", id);
                return new PriceChartDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar gráfico de {Id}", id);
                return new PriceChartDto();
            }
        }

        // ===== TAXAS DE CÂMBIO =====
        public async Task<ExchangeRateDto> GetExchangeRatesAsync(string baseCurrency, params string[] symbols)
        {
            if (string.IsNullOrWhiteSpace(baseCurrency) || symbols is null || !symbols.Any())
            {
                return new ExchangeRateDto { Rates = new Dictionary<string, Dictionary<string, decimal>>() };
            }

            var client = _httpFactory.CreateClient("CoinGecko");
            var ids = string.Join(",", symbols);
            var url = $"simple/price?ids={ids}&vs_currencies={baseCurrency}";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var rates = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, decimal>>>(json);

            return new ExchangeRateDto { Rates = rates };
        }

        // ===== FAVORITOS =====
        public Task AddFavoriteAsync(string cryptoId)
        {
            var favorites = GetFavoriteIds();
            if (favorites.Add(cryptoId))
            {
                _cache.Set(FavoritesKey, favorites);
                _logger.LogInformation("⭐ {CryptoId} adicionado aos favoritos", cryptoId);
            }
            return Task.CompletedTask;
        }

        public Task RemoveFavoriteAsync(string cryptoId)
        {
            var favorites = GetFavoriteIds();
            if (favorites.Remove(cryptoId))
            {
                _cache.Set(FavoritesKey, favorites);
                _logger.LogInformation("⭐ {CryptoId} removido dos favoritos", cryptoId);
            }
            return Task.CompletedTask;
        }

        public async Task<List<CryptoCurrency>> GetFavoritesAsync()
        {
            var allCryptos = await GetCryptosAsync();
            return allCryptos.Where(c => c.IsFavorite).ToList();
        }

        private HashSet<string> GetFavoriteIds()
        {
            return _cache.Get<HashSet<string>>(FavoritesKey) ?? new HashSet<string>();
        }

        // ===== ALERTAS =====
        public Task AddAlertAsync(AlertDto alert)
        {
            _alerts.Add(alert);
            _logger.LogInformation("🔔 Alerta adicionado para {CryptoId} com alvo {TargetPrice}", alert.CryptoId, alert.TargetPrice);
            return Task.CompletedTask;
        }

        public Task RemoveAlertAsync(Guid alertId)
        {
            var removedCount = _alerts.RemoveAll(a => a.Id == alertId);
            if (removedCount > 0)
                _logger.LogInformation("🔔 Alerta {AlertId} removido", alertId);
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
                return;

            var triggeredAlerts = new List<AlertDto>();

            foreach (var alert in _alerts.ToList())
            {
                var crypto = cryptos.FirstOrDefault(c => c.Id == alert.CryptoId);
                if (crypto == null) continue;

                bool triggered = (alert.Type.Equals("max", StringComparison.OrdinalIgnoreCase) && crypto.PriceUsd >= alert.TargetPrice) ||
                                 (alert.Type.Equals("min", StringComparison.OrdinalIgnoreCase) && crypto.PriceUsd <= alert.TargetPrice);

                if (triggered)
                {
                    _logger.LogWarning("🚨 ALERTA! {CryptoName} atingiu {TargetPrice}. Preço atual: {CurrentPrice}",
                        crypto.Name, alert.TargetPrice, crypto.PriceUsd);

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
                _logger.LogInformation("🔔 {Count} alertas disparados e removidos", triggeredAlerts.Count);
            }
        }
    }
}