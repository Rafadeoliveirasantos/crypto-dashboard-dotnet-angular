using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using CryptoDashboard.Application.Services;

namespace CryptoDashboard.Infrastructure.HostedServices
{
    public class CryptoBackgroundService : IHostedService, IDisposable
    {
        private readonly ICryptoService _cryptoService;
        private readonly ILogger<CryptoBackgroundService> _logger;
        private Timer? _timer;

        public CryptoBackgroundService(ICryptoService cryptoService, ILogger<CryptoBackgroundService> logger)
        {
            _cryptoService = cryptoService;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CryptoBackgroundService iniciado.");
            _timer = new Timer(UpdateData, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
            return Task.CompletedTask;
        }

        private async void UpdateData(object? state)
        {
            try
            {
                _logger.LogInformation("Atualizando criptomoedas e taxas de câmbio...");
                await _cryptoService.GetCryptosAsync();
                await _cryptoService.GetExchangeRatesAsync("USD", "BRL", "EUR");
                _logger.LogInformation("Atualização concluída.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar dados no CryptoBackgroundService");
            }
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