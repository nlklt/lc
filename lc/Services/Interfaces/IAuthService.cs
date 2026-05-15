using lc.Models;

namespace lc.Services.Interfaces
{
    public interface IAuthService
    {
        Task<User?> LoginAsync(string userName, string password);
        Task<User> RegisterAsync(string userName, string password);
        Task ApplyUserSettingsAsync(User user);
        void Logout();
    }
}