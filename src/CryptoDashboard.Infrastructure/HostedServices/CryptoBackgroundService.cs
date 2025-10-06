using CryptoDashboard.Application.Services;
using Microsoft.Extensions.Configuration; // Adicionado para ler configurações
using Microsoft.Extensions.DependencyInjection; // Adicionado para escopo de serviço
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

        // --- CONSTRUTOR ATUALIZADO ---
        // Injeta as dependências necessárias:
        // IServiceScopeFactory: Para criar um escopo de serviço seguro a cada execução.
        // IConfiguration: Para ler o arquivo appsettings.json.
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
            _logger.LogInformation("CryptoBackgroundService iniciado.");

            // Lê o intervalo de atualização do appsettings.json.
            // Se não encontrar, usa 60 segundos como padrão.
            var updateInterval = _configuration.GetValue<int>("AppSettings:UpdateIntervalSeconds", 60);

            _logger.LogInformation($"Intervalo de atualização definido para {updateInterval} segundos.");

            // Inicia o timer com o intervalo lido da configuração.
            _timer = new Timer(UpdateData, null, TimeSpan.Zero, TimeSpan.FromSeconds(updateInterval));

            return Task.CompletedTask;
        }

        // --- MÉTODO PRINCIPAL ATUALIZADO ---
        private async void UpdateData(object? state)
        {
            _logger.LogInformation("Iniciando ciclo de atualização em background...");

            try
            {
                // Cria um "escopo" para esta execução, garantindo que os serviços (como o ICryptoService)
                // sejam usados de forma segura em um serviço de longa duração (Singleton).
                using (var scope = _scopeFactory.CreateScope())
                {
                    var cryptoService = scope.ServiceProvider.GetRequiredService<ICryptoService>();

                    // 1. Atualiza os dados das criptomoedas no cache
                    _logger.LogInformation("Atualizando dados das criptomoedas...");
                    await cryptoService.GetCryptosAsync();
                    _logger.LogInformation("Dados atualizados com sucesso.");

                    // 2. Processa os alertas de preço com os dados recém-atualizados
                    _logger.LogInformation("Processando alertas de preço...");
                    await cryptoService.ProcessAlertsAsync();
                    _logger.LogInformation("Alertas processados com sucesso.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro inesperado durante o ciclo de atualização em background.");
            }

            _logger.LogInformation("Ciclo de atualização em background finalizado.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CryptoBackgroundService parado.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}