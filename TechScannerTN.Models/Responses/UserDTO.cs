using Hi_Trade.DAL.Entities;
using Hi_Trade.Models.Common;
using System;

namespace Hi_Trade.Models.Responses;

public class UserDTO : BaseDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public decimal Balance { get; set; } = 0;
    public Roles Role { get; set; } = Roles.User;
    public string Token { get; set; } = string.Empty;
    public DateTime? Expires { get; set; } = null;
}

