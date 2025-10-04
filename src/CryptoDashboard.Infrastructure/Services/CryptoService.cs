using CryptoDashboard.Application.Services;
using CryptoDashboard.Domain;
using CryptoDashboard.Dto.Crypto;
using CryptoDashboard.Dto;
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

            var cachedCryptos = _cache.Get<List<CryptoCurrency>>(cacheKey);
            List<CryptoCurrency> cryptos;

            if (cachedCryptos is not null)
            {
                cryptos = cachedCryptos;
            }
            else
            {
                var client = _httpFactory.CreateClient();
                client.DefaultRequestHeaders.Add("User-Agent", "CryptoDashboardApp/1.0");

                // USD
                var urlUsd = "https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=market_cap_desc&per_page=10&page=1&sparkline=false";
                var responseUsd = await client.GetAsync(urlUsd);
                responseUsd.EnsureSuccessStatusCode();
                var jsonUsd = await responseUsd.Content.ReadAsStringAsync();
                var listUsd = JsonSerializer.Deserialize<List<CoinGeckoResponseDto>>(jsonUsd) ?? new();

                // BRL
                var urlBrl = "https://api.coingecko.com/api/v3/coins/markets?vs_currency=brl&order=market_cap_desc&per_page=10&page=1&sparkline=false";
                var responseBrl = await client.GetAsync(urlBrl);
                responseBrl.EnsureSuccessStatusCode();
                var jsonBrl = await responseBrl.Content.ReadAsStringAsync();
                var listBrl = JsonSerializer.Deserialize<List<CoinGeckoResponseDto>>(jsonBrl) ?? new();

                var favoritesSet = GetFavoriteIds();

                // Combina USD + BRL (assumindo mesma ordem, id)
                cryptos = listUsd.Select(dto =>
                {
                    var brlDto = listBrl.FirstOrDefault(b => b.Id == dto.Id);
                    return new CryptoCurrency
                    {
                        Id = dto.Id,
                        Name = dto.Name,
                        Symbol = dto.Symbol,
                        ImageUrl = dto.Image,
                        PriceUsd = dto.CurrentPrice,
                        MarketCap = dto.MarketCap,
                        PriceBrl = brlDto?.CurrentPrice ?? 0,
                        Variation24h = dto.PriceChangePercentage24h,
                        Volume24h = dto.TotalVolume,
                        LastUpdated = dto.LastUpdated,
                        Trend7d = dto.SparklineIn7d?.Price ?? new List<decimal>(),
                        IsFavorite = favoritesSet.Contains(dto.Id)
                    };
                }).ToList();

                _cache.Set(cacheKey, cryptos, TimeSpan.FromMinutes(5));
            }

            // Atualiza favoritos caso tenha mudado
            var favorites = GetFavoriteIds();
            foreach (var crypto in cryptos)
                crypto.IsFavorite = favorites.Contains(crypto.Id);

            // --------------------------
            // FILTROS E ORDENAÇÃO
            // --------------------------
            IEnumerable<CryptoCurrency> query = cryptos;

            // Busca por nome/símbolo
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c =>
                    c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.Symbol.Contains(search, StringComparison.OrdinalIgnoreCase));

            // Filtro de preço
            if (minPrice.HasValue)
                query = query.Where(c => c.PriceUsd >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(c => c.PriceUsd <= maxPrice.Value);

            // Filtro por variação
            if (!string.IsNullOrWhiteSpace(variation))
            {
                if (variation.Equals("positive", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(c => c.Variation24h > 0);
                else if (variation.Equals("negative", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(c => c.Variation24h < 0);
            }

            // Ordenação
            bool desc = direction?.ToLower() != "asc";
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                switch (orderBy.ToLower())
                {
                    case "marketcap":
                        query = desc ? query.OrderByDescending(c => c.MarketCap) : query.OrderBy(c => c.MarketCap);
                        break;
                    case "price":
                        query = desc ? query.OrderByDescending(c => c.PriceUsd) : query.OrderBy(c => c.PriceUsd);
                        break;
                    case "volume":
                        query = desc ? query.OrderByDescending(c => c.Volume24h) : query.OrderBy(c => c.Volume24h);
                        break;
                    case "variation":
                        query = desc ? query.OrderByDescending(c => c.Variation24h) : query.OrderBy(c => c.Variation24h);
                        break;
                    case "name":
                        query = desc ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name);
                        break;
                    default:
                        query = desc ? query.OrderByDescending(c => c.MarketCap) : query.OrderBy(c => c.MarketCap);
                        break;
                }
            }

            return query.ToList();
        }

        public async Task<CryptoDetailDto> GetCryptoDetailAsync(string id)
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "CryptoDashboardApp/1.0");
            var url = $"https://api.coingecko.com/api/v3/coins/{id}";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var detail = JsonSerializer.Deserialize<CryptoDetailDto>(json) ?? new CryptoDetailDto();
            return detail;
        }

        public async Task<PriceChartDto> GetPriceChartAsync(string id, int days)
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "CryptoDashboardApp/1.0");
            var url = $"https://api.coingecko.com/api/v3/coins/{id}/market_chart?vs_currency=usd&days={days}";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var chart = JsonSerializer.Deserialize<PriceChartDto>(json) ?? new PriceChartDto();
            return chart;
        }


        public async Task<ExchangeRateDto> GetExchangeRatesAsync(string baseCurrency, params string[] symbols)
        {
            var cacheKey = $"exchange_rates_{baseCurrency}_{string.Join("_", symbols)}";
            var cachedRates = _cache.Get<ExchangeRateDto>(cacheKey);
            if (cachedRates is not null)
            {
                return cachedRates;
            }

            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "CryptoDashboardApp/1.0");

            var url = $"https://economia.awesomeapi.com.br/json/last/{string.Join(",", symbols.Select(s => $"{baseCurrency}-{s}"))}";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            var ratesDict = JsonSerializer.Deserialize<Dictionary<string, CurrencyRateDto>>(json) ?? new();
            var rates = new ExchangeRateDto
            {
                Rates = ratesDict
            };

            _cache.Set(cacheKey, rates, TimeSpan.FromMinutes(10));

            return rates;
        }

        // FAVORITOS

        public Task AddFavoriteAsync(string cryptoId)
        {
            var favorites = GetFavoriteIds();
            favorites.Add(cryptoId);
            _cache.Set(FavoritesKey, favorites);
            return Task.CompletedTask;
        }

        public Task RemoveFavoriteAsync(string cryptoId)
        {
            var favorites = GetFavoriteIds();
            favorites.Remove(cryptoId);
            _cache.Set(FavoritesKey, favorites);
            return Task.CompletedTask;
        }

        public async Task<List<CryptoCurrency>> GetFavoritesAsync()
        {
            var allCryptos = await GetCryptosAsync();
            var favorites = GetFavoriteIds();
            return allCryptos.Where(c => favorites.Contains(c.Id)).ToList();
        }

        private HashSet<string> GetFavoriteIds()
        {
            return _cache.Get<HashSet<string>>(FavoritesKey) ?? new HashSet<string>();
        }

        // ALERTAS

        public Task AddAlertAsync(AlertDto alert)
        {
            _alerts.Add(alert);
            return Task.CompletedTask;
        }

        public Task RemoveAlertAsync(Guid alertId)
        {
            _alerts.RemoveAll(a => a.Id == alertId);
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
    }
}