using System;

namespace Hi_Trade.DAL.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public decimal Balance { get; set; } = 0;
    public Roles Role { get; set; } = Roles.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

