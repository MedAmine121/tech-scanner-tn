using Hi_Trade.BLL.BLL;
using Hi_Trade.BLL.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Hi_Trade.BLL.DI;

public static class DIBLLExtension
{
    public static IServiceCollection AddBLLServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenBLL, TokenBLL>()
                .AddScoped<IHiTradeBLL, HiTradeBLL>();
        return services;
    }
}

