using RiotProxy.External.Domain.Entities;
using RiotProxy.External.Domain.Enums;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

public interface IUserRepository
{
    Task<IList<User>> GetAllUsersAsync();
    Task<User?> GetByUserNameAsync(string userName);
    Task<User?> CreateUserAsync(string userName, UserTypeEnum userType);
}
