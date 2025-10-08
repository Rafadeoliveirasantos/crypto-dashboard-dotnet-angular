using CryptoDashboard.Application.Services;
using CryptoDashboard.Infrastructure.HostedServices;
using CryptoDashboard.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using CryptoDashboard.Application.Mapping;

namespace CryptoDashboard.IoC.DependencyInjection
{
    /// <summary>
    /// Extensões para configuração de injeção de dependência do CryptoDashboard
    /// Autor: Rafadeoliveirasantos
    /// Data: 2025-10-08 18:24:21 UTC
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adiciona todas as dependências necessárias para o CryptoDashboard API
        /// Inclui: Services, BackgroundServices, Cache e AutoMapper
        /// </summary>
        /// <param name="services">Coleção de serviços do ASP.NET Core</param>
        /// <returns>IServiceCollection para encadeamento de métodos</returns>
        public static IServiceCollection AddDashboardDependencies(this IServiceCollection services)
        {
            // ===== MEMORY CACHE =====
            // Cache para armazenamento temporário de dados de criptomoedas e configurações
            services.AddMemoryCache();

            // ===== APPLICATION SERVICES =====
            // CryptoService: Gerencia operações com criptomoedas (busca, cache, favoritos)
            services.AddScoped<ICryptoService, CryptoService>();

            // SettingsService: Gerencia configurações do sistema (intervalo, moeda, cache)
            services.AddScoped<ISettingsService, SettingsService>();

            // ===== BACKGROUND SERVICES =====
            // CryptoBackgroundService: Atualiza dados de criptomoedas automaticamente
            services.AddHostedService<CryptoBackgroundService>();

            // ===== AUTOMAPPER =====
            // Mapeamento automático entre DTOs e Entities
            services.AddAutoMapper(typeof(CryptoMappingProfile).Assembly);

            return services;
        }
    }
}