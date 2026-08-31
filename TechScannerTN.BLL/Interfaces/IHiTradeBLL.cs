using Hi_Trade.Models.Requests;
using Hi_Trade.Models.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace Hi_Trade.BLL.Interfaces;

public interface IHiTradeBLL
{
    Task<UserDTO> CreateUser(CreateUserRequest request, CancellationToken ct);
    Task<UserDTO?> LoginUser(LoginUserRequest request, CancellationToken ct);
    Task<UserDTO> FetchUser(string email, CancellationToken ct);
}

