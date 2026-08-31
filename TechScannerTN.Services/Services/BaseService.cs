using FluentValidation;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Hi_Trade.Services.Services;

public class BaseService(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public virtual async Task<Response?> Validate<Request, Response>(Func<Request, CancellationToken, Task<Response?>> invoke, Request request, CancellationToken ct) where Response : class
    {
        var validator = _serviceProvider.GetService(typeof(IValidator<Request>)) as IValidator<Request>;
        if (validator != null)
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }
        return await invoke(request, ct);
    }

    protected static string GetUserEmailFromToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        token = token.Replace("Bearer ", "").Trim();
        var jwtToken = handler.ReadJwtToken(token);
        string? email = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.Email || c.Type == "email" || c.Type == "sub")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            throw new Exception("Email not found in JWT token");
        }
        return email;
    }
}

