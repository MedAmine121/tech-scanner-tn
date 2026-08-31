using Hi_Trade.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hi_Trade.DAL;

public class HiTradeDAL(TechScannerContext context) : IHiTradeDAL
{
    public async Task<User> CreateUser(string email, string password, string fullName, string address, CancellationToken ct)
    {
        if (await context.Users.AnyAsync(u => u.Email == email, ct))
        {
            throw new Exception("User with the same email already exists.");
        }

        var user = new User
        {
            Email = email,
            Password = password,
            FullName = fullName,
            Address = address,
            Role = Roles.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        var result = await context.SaveChangesAsync(ct);
        if (result > 0)
        {
            return user;
        }

        throw new Exception("Failed to create user.");
    }

    public async Task<User?> LoginUser(string email, CancellationToken ct)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<User> FetchUser(string email, CancellationToken ct)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user == null)
        {
            throw new Exception("User not found.");
        }
        return user;
    }
}

