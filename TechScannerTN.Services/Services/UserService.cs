using FluentValidation;
using Hi_Trade.BLL.Interfaces;
using Hi_Trade.Models.Common;
using Hi_Trade.Models.Requests;
using Hi_Trade.Models.Responses;
using Hi_Trade.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hi_Trade.Services.Services;

public class UserService(IHiTradeBLL hiTradeBLL, ITokenBLL tokenBLL, ILogger<UserService> logger, IServiceProvider serviceProvider) 
    : BaseService(serviceProvider), IUserService
{
    public async Task<BaseResult<UserDTO>> CreateUser(CreateUserRequest request, CancellationToken ct)
    {
        try
        {
            UserDTO? user = await Validate(hiTradeBLL.CreateUser!, request, ct);
            if (user != null)
            {
                user = tokenBLL.GenerateJwtToken(user);
            }
            return new BaseResult<UserDTO>
            {
                Model = user,
                ResultType = ResultType.Success,
                Message = "User registered successfully."
            };
        }
        catch (ValidationException vex)
        {
            logger.LogWarning(vex, "Validation failed while creating user.");
            var errorMessages = string.Join("; ", vex.Errors.Select(e => e.ErrorMessage));
            return new BaseResult<UserDTO>
            {
                Model = null,
                ResultType = ResultType.BadRequest,
                Message = !string.IsNullOrEmpty(errorMessages) ? errorMessages : vex.Message
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while creating user.");
            return new BaseResult<UserDTO>
            {
                Model = null,
                ResultType = ResultType.Fail,
                Message = ex.Message
            };
        }
    }

    public async Task<BaseResult<UserDTO>> LoginUser(LoginUserRequest request, CancellationToken ct)
    {
        try
        {
            UserDTO? user = await Validate(hiTradeBLL.LoginUser!, request, ct);
            if (user != null)
            {
                user = tokenBLL.GenerateJwtToken(user);
                return new BaseResult<UserDTO>
                {
                    Model = user,
                    ResultType = ResultType.Success,
                    Message = "Login successful."
                };
            }
            else
            {
                return new BaseResult<UserDTO>
                {
                    Model = null,
                    ResultType = ResultType.Fail,
                    Message = "Invalid email or password."
                };
            }
        }
        catch (ValidationException vex)
        {
            logger.LogWarning(vex, "Validation failed while logging in.");
            var errorMessages = string.Join("; ", vex.Errors.Select(e => e.ErrorMessage));
            return new BaseResult<UserDTO>
            {
                Model = null,
                ResultType = ResultType.BadRequest,
                Message = !string.IsNullOrEmpty(errorMessages) ? errorMessages : vex.Message
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while logging in.");
            return new BaseResult<UserDTO>
            {
                Model = null,
                ResultType = ResultType.Error,
                Message = ex.Message
            };
        }
    }

    public async Task<BaseResult<SaveResponse>> LogoutUser(string token)
    {
        try
        {
            // Stateless JWT logout (client discards token, response confirms logout)
            return await Task.FromResult(new BaseResult<SaveResponse>
            {
                Model = new SaveResponse { Success = true, Message = "Logout successful." },
                ResultType = ResultType.Success
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while logging out.");
            return new BaseResult<SaveResponse>
            {
                Model = new SaveResponse { Success = false, Message = ex.Message },
                ResultType = ResultType.Error,
                Message = ex.Message
            };
        }
    }

    public async Task<BaseResult<UserDTO>> FetchUser(string token, CancellationToken ct)
    {
        try
        {
            string email = GetUserEmailFromToken(token);
            UserDTO user = await hiTradeBLL.FetchUser(email, ct);
            return new BaseResult<UserDTO>
            {
                Model = user,
                ResultType = ResultType.Success
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while fetching user profile.");
            return new BaseResult<UserDTO>
            {
                Model = null,
                ResultType = ResultType.Error,
                Message = ex.Message
            };
        }
    }
}

