using Hi_Trade.Models.Responses;

namespace Hi_Trade.BLL.Interfaces;

public interface ITokenBLL
{
    UserDTO GenerateJwtToken(UserDTO user);
}

