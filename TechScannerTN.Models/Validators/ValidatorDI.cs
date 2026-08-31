using FluentValidation;
using Hi_Trade.Models.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace Hi_Trade.Models.Validators;

public static class ValidatorDI
{
    public static IServiceCollection RegisterValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserValidator>()
                .AddScoped<IValidator<LoginUserRequest>, LoginUserValidator>();
        return services;
    }
}

