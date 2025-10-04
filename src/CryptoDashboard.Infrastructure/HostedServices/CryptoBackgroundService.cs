using CryptoDashboard.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class CryptoDataRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CryptoDataRefreshService> _logger;

    public CryptoDataRefreshService(IServiceProvider serviceProvider, ILogger<CryptoDataRefreshService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var cryptoService = scope.ServiceProvider.GetRequiredService<ICryptoService>();

                    // Atualiza os dados e cache
                    await cryptoService.GetCryptosAsync();
                    await cryptoService.GetExchangeRatesAsync("USD", "BRL", "EUR"); // Adapte os símbolos conforme o uso

                    _logger.LogInformation("Dados de criptomoedas e taxas atualizados às {time}", DateTimeOffset.Now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar dados de criptomoedas!");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}