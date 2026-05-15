using lc.Models;

namespace lc.Infrastructure.Repositories.Abstractions
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int userId);
        Task<User?> GetByUserNameAsync(string userName);
        Task<bool> ExistsByUserNameAsync(string userName);

        Task<int> CreateAsync(User user);
        Task<bool> UpdateAsync(User user);
        Task DeleteAsync(int userId);
    }
}