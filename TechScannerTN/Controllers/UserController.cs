using Hi_Trade.Models.Common;
using Hi_Trade.Models.Requests;
using Hi_Trade.Models.Responses;
using Hi_Trade.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace Hi_Trade.Controllers;

[ApiController]
[Route("user")]
public class UserController(IUserService userService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<BaseResult<UserDTO>> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        return await userService.CreateUser(request, ct);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<BaseResult<UserDTO>> LoginUser([FromBody] LoginUserRequest request, CancellationToken ct)
    {
        return await userService.LoginUser(request, ct);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<BaseResult<SaveResponse>> LogoutUser([FromHeader(Name = "Authorization")] string token)
    {
        return await userService.LogoutUser(token);
    }

    [Authorize]
    [HttpGet("fetch")]
    public async Task<BaseResult<UserDTO>> FetchUser([FromHeader(Name = "Authorization")] string token, CancellationToken ct)
    {
        return await userService.FetchUser(token, ct);
    }
}

