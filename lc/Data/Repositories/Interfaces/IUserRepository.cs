using lc.Models;

namespace lc.Infrastructure.Repositories.Abstractions
{
    public interface IUserRepository
    {
        Task<int> CreateAsync(User user);
        Task<User?> GetByIdAsync(int userId);
        Task<bool> ExistsByUserNameAsync(string userName);
        Task<User?> GetByUserNameAsync(string userName);
        Task<bool> UpdateAsync(User user);
        Task DeleteAsync(int userId);

        Task<User?> AuthenticateAsync(string userName, string password);
        Task<User> RegisterAsync(string userName, string password);
    }
}