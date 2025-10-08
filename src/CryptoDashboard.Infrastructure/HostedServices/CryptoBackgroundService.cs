using CryptoDashboard.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoDashboard.Infrastructure.HostedServices
{
    public class CryptoBackgroundService : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CryptoBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private Timer? _timer;
        private int _executionCount = 0;

        public CryptoBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<CryptoBackgroundService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🚀 CryptoBackgroundService iniciado");

            // 🆕 Lê o intervalo do appsettings.json (padrão: 5 minutos)
            // Mudamos de 60s para 300s (5 min) para respeitar o rate limit
            var updateIntervalSeconds = _configuration.GetValue<int>("AppSettings:UpdateIntervalSeconds", 300);

            // 🆕 Validação: Mínimo de 120 segundos (2 minutos)
            if (updateIntervalSeconds < 120)
            {
                _logger.LogWarning("⚠️ Intervalo muito curto ({Seconds}s). Ajustando para 120s para evitar rate limit.",
                    updateIntervalSeconds);
                updateIntervalSeconds = 120;
            }

            _logger.LogInformation("⏱️ Intervalo de atualização: {Interval} segundos ({Minutes} minutos)",
                updateIntervalSeconds,
                updateIntervalSeconds / 60.0);

            // 🆕 Primeira execução após 10 segundos (não imediatamente)
            // Isso dá tempo para o sistema inicializar completamente
            _timer = new Timer(
                UpdateData,
                null,
                TimeSpan.FromSeconds(10), // Delay inicial: 10 segundos
                TimeSpan.FromSeconds(updateIntervalSeconds) // Intervalo entre execuções
            );

            return Task.CompletedTask;
        }

        private async void UpdateData(object? state)
        {
            _executionCount++;
            var executionId = _executionCount;

            _logger.LogInformation("🔄 [{ExecutionId}] Iniciando ciclo de atualização em background...", executionId);

            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var cryptoService = scope.ServiceProvider.GetRequiredService<ICryptoService>();

                    // 🆕 1. Atualiza os dados das criptomoedas
                    _logger.LogInformation("📊 [{ExecutionId}] Atualizando dados das criptomoedas...", executionId);

                    var startTime = DateTime.UtcNow;
                    var cryptos = await cryptoService.GetCryptosAsync();
                    var duration = DateTime.UtcNow - startTime;

                    if (cryptos != null && cryptos.Any())
                    {
                        _logger.LogInformation(
                            "✅ [{ExecutionId}] {Count} criptomoedas atualizadas em {Duration}ms",
                            executionId,
                            cryptos.Count,
                            duration.TotalMilliseconds);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "⚠️ [{ExecutionId}] Nenhuma criptomoeda retornada (pode estar usando cache de backup)",
                            executionId);
                    }

                    // 🆕 2. Processa os alertas de preço
                    _logger.LogInformation("🔔 [{ExecutionId}] Processando alertas de preço...", executionId);

                    await cryptoService.ProcessAlertsAsync();

                    _logger.LogInformation("✅ [{ExecutionId}] Alertas processados com sucesso", executionId);
                }

                _logger.LogInformation(
                    "✅ [{ExecutionId}] Ciclo de atualização concluído com sucesso",
                    executionId);
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning(
                    "⚠️ [{ExecutionId}] Rate limit atingido (429). Dados em cache serão usados até a próxima atualização.",
                    executionId);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(
                    httpEx,
                    "❌ [{ExecutionId}] Erro HTTP durante atualização: {StatusCode} - {Message}",
                    executionId,
                    httpEx.StatusCode,
                    httpEx.Message);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning(
                    "⚠️ [{ExecutionId}] Timeout ao buscar dados da API. A próxima execução tentará novamente.",
                    executionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ [{ExecutionId}] Erro inesperado durante o ciclo de atualização: {ErrorType} - {Message}",
                    executionId,
                    ex.GetType().Name,
                    ex.Message);
            }

            _logger.LogInformation(
                "🏁 [{ExecutionId}] Ciclo de atualização finalizado. Próxima execução em {NextRun}",
                executionId,
                GetNextExecutionTime());
        }

        // 🆕 Método auxiliar para calcular próxima execução
        private string GetNextExecutionTime()
        {
            var intervalSeconds = _configuration.GetValue<int>("AppSettings:UpdateIntervalSeconds", 300);
            var nextRun = DateTime.UtcNow.AddSeconds(intervalSeconds);
            return nextRun.ToString("HH:mm:ss");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 CryptoBackgroundService parando...");

            _timer?.Change(Timeout.Infinite, 0);

            _logger.LogInformation(
                "✅ CryptoBackgroundService parado após {ExecutionCount} execuções",
                _executionCount);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _logger.LogInformation("🗑️ CryptoBackgroundService disposed");
        }
    }
}