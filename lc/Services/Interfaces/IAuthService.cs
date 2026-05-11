using lc.Models;

namespace lc.Services.Interfaces
{
    public interface IAuthService
    {
        Task<User?> LoginAsync(string userName, string password);
        Task<User> RegisterAsync(string userName, string password);
        void Logout();
        Task ApplyUserSettingsAsync(User user);
    }
}