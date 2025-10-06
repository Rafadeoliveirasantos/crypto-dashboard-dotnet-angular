using CryptoDashboard.Application.Services;
using CryptoDashboard.Infrastructure.HostedServices;
using CryptoDashboard.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using CryptoDashboard.Application.Mapping;


namespace CryptoDashboard.IoC.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDashboardDependencies(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<ICryptoService, CryptoService>();
            services.AddHostedService<CryptoBackgroundService>();
            services.AddScoped<ISettingsService, SettingsService>();
            services.AddAutoMapper(typeof(CryptoMappingProfile).Assembly);
            return services;
        }
    }
}