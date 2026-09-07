using Hi_Trade.Services.Interfaces;
using Hi_Trade.Services.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hi_Trade.Services.DI;

public static class DIServiceExtension
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProductIngestionService, ProductIngestionService>();
        services.AddScoped<IProductCatalogService, ProductCatalogService>();
        return services;
    }
}

