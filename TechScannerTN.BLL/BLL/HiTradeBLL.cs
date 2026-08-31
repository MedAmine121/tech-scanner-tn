using BCrypt.Net;
using Hi_Trade.BLL.Interfaces;
using Hi_Trade.DAL;
using Hi_Trade.DAL.Entities;
using Hi_Trade.Models.Requests;
using Hi_Trade.Models.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hi_Trade.BLL.BLL;

public class HiTradeBLL(IHiTradeDAL hiTradeDAL) : IHiTradeBLL
{
    public async Task<UserDTO> CreateUser(CreateUserRequest request, CancellationToken ct)
    {
        var hashedPassword = HashPassword(request.Password);
        var user = await hiTradeDAL.CreateUser(request.Email, hashedPassword, request.FullName, request.Address, ct);
        return new UserDTO
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Address = user.Address,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Balance = user.Balance,
            Role = user.Role
        };
    }

    public async Task<UserDTO?> LoginUser(LoginUserRequest request, CancellationToken ct)
    {
        var user = await hiTradeDAL.LoginUser(request.Email, ct);
        if (user == null)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            return null;
        }

        return new UserDTO
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Address = user.Address,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Balance = user.Balance,
            Role = user.Role
        };
    }

    public async Task<UserDTO> FetchUser(string email, CancellationToken ct)
    {
        var user = await hiTradeDAL.FetchUser(email, ct);
        return new UserDTO
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Address = user.Address,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Balance = user.Balance,
            Role = user.Role
        };
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}

