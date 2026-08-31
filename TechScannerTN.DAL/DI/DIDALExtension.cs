using Microsoft.Extensions.DependencyInjection;

namespace Hi_Trade.DAL.DI;

public static class DIDALExtension
{
    public static IServiceCollection AddDALServices(this IServiceCollection services)
    {
        services.AddScoped<IHiTradeDAL, HiTradeDAL>();
        return services;
    }
}

