using Hi_Trade.BLL.Interfaces;
using Hi_Trade.Models.Common;
using Hi_Trade.Models.Responses;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Hi_Trade.BLL.BLL;

public class TokenBLL(IOptions<JWTOptions> options) : ITokenBLL
{
    public UserDTO GenerateJwtToken(UserDTO user)
    {
        var secret = options.Value.Secret;
        if (string.IsNullOrEmpty(secret))
        {
            secret = "TechScannerTN_SuperSecretKey_2026_SecureAuthentication_DevKey";
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expirationMinutes = options.Value.ExpirationMinutes > 0 ? options.Value.ExpirationMinutes : 1440;
        var expires = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = options.Value.Issuer,
            Audience = options.Value.Audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        user.Token = tokenHandler.WriteToken(token);
        user.Expires = expires;
        return user;
    }
}

