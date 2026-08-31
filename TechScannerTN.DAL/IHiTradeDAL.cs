using Hi_Trade.DAL.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Hi_Trade.DAL;

public interface IHiTradeDAL
{
    Task<User> CreateUser(string email, string password, string fullName, string address, CancellationToken ct);
    Task<User?> LoginUser(string email, CancellationToken ct);
    Task<User> FetchUser(string email, CancellationToken ct);
}

