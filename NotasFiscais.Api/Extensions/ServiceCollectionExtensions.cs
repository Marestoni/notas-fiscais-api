using NotasFiscais.Api.Repositories;
using NotasFiscais.Api.Services;

namespace NotasFiscais.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotasFiscais(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<INotaFiscalRepository, NotaFiscalMemoryRepository>();
        services.AddScoped<INotaFiscalService, NotaFiscalService>();
        return services;
    }
}
