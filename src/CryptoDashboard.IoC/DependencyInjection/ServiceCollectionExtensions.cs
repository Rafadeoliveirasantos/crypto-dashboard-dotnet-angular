using CryptoDashboard.Application.Services;
using CryptoDashboard.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;


namespace CryptoDashboard.IoC.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDashboardDependencies(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddMemoryCache();
            services.AddScoped<ICryptoService, CryptoService>();
            return services;
        }
    }
}