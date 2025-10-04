using Microsoft.Extensions.DependencyInjection;
using CryptoDashboard.Application.Services;

namespace CryptoDashboard.IoC.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDashboardDependencies(this IServiceCollection services)
        {
            services.AddScoped<UserService>();
            return services;
        }
    }
}