using Hi_Trade.BLL.BLL;
using Hi_Trade.BLL.Interfaces;
using Hi_Trade.DAL;
using Hi_Trade.DAL.Entities;
using Hi_Trade.Models.Common;
using Hi_Trade.Models.Requests;
using Hi_Trade.Models.Responses;
using Hi_Trade.Models.Validators;
using Hi_Trade.Services.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hi_Trade.Tests;

public class AuthTests
{
    [Fact]
    public void TokenBLL_GenerateJwtToken_SetsTokenAndExpiry()
    {
        // Arrange
        var jwtOptions = Options.Create(new JWTOptions
        {
            Secret = "SuperSecretKeyForTestingAuthTokens123456!",
            Issuer = "TechScannerTN",
            Audience = "TechScannerTN.Client",
            ExpirationMinutes = 60
        });
        var tokenBll = new TokenBLL(jwtOptions);
        var user = new UserDTO
        {
            Id = 1,
            Email = "tester@techscanner.tn",
            FullName = "Tester TN",
            Role = Roles.User
        };

        // Act
        var result = tokenBll.GenerateJwtToken(user);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.NotNull(result.Expires);
        Assert.True(result.Expires > DateTime.UtcNow);
    }

    [Fact]
    public async Task HiTradeBLL_CreateUser_HashesPasswordAndCallsDAL()
    {
        // Arrange
        var mockDal = new Mock<IHiTradeDAL>();
        var request = new CreateUserRequest
        {
            Email = "newuser@techscanner.tn",
            Password = "SecurePassword123!",
            FullName = "New User",
            Address = "Tunis"
        };

        mockDal.Setup(d => d.CreateUser(
            request.Email,
            It.Is<string>(p => p != request.Password), // Hashed
            request.FullName,
            request.Address,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 10,
                Email = request.Email,
                FullName = request.FullName,
                Address = request.Address,
                Role = Roles.User
            });

        var bll = new HiTradeBLL(mockDal.Object);

        // Act
        var result = await bll.CreateUser(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal(request.Email, result.Email);
        mockDal.Verify(d => d.CreateUser(request.Email, It.IsAny<string>(), request.FullName, request.Address, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HiTradeBLL_LoginUser_ValidCredentials_ReturnsUserDTO()
    {
        // Arrange
        var mockDal = new Mock<IHiTradeDAL>();
        var password = "CorrectPassword123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        mockDal.Setup(d => d.LoginUser("valid@techscanner.tn", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 1,
                Email = "valid@techscanner.tn",
                Password = hashedPassword,
                FullName = "Valid User",
                Role = Roles.User
            });

        var bll = new HiTradeBLL(mockDal.Object);

        // Act
        var result = await bll.LoginUser(new LoginUserRequest
        {
            Email = "valid@techscanner.tn",
            Password = password
        }, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("valid@techscanner.tn", result.Email);
    }

    [Fact]
    public async Task HiTradeBLL_LoginUser_WrongPassword_ReturnsNull()
    {
        // Arrange
        var mockDal = new Mock<IHiTradeDAL>();
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");

        mockDal.Setup(d => d.LoginUser("valid@techscanner.tn", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 1,
                Email = "valid@techscanner.tn",
                Password = hashedPassword,
                FullName = "Valid User"
            });

        var bll = new HiTradeBLL(mockDal.Object);

        // Act
        var result = await bll.LoginUser(new LoginUserRequest
        {
            Email = "valid@techscanner.tn",
            Password = "WrongPassword"
        }, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CreateUserValidator_ValidatesRules()
    {
        var validator = new CreateUserValidator();

        // Valid
        var validReq = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "ValidPassword123",
            FullName = "John Doe",
            Address = "Tunis"
        };
        var validResult = validator.Validate(validReq);
        Assert.True(validResult.IsValid);

        // Invalid email & short password
        var invalidReq = new CreateUserRequest
        {
            Email = "not-an-email",
            Password = "123",
            FullName = "",
            Address = ""
        };
        var invalidResult = validator.Validate(invalidReq);
        Assert.False(invalidResult.IsValid);
        Assert.Contains(invalidResult.Errors, e => e.PropertyName == "Email");
        Assert.Contains(invalidResult.Errors, e => e.PropertyName == "Password");
        Assert.Contains(invalidResult.Errors, e => e.PropertyName == "FullName");
        Assert.Contains(invalidResult.Errors, e => e.PropertyName == "Address");
    }
}

