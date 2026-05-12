using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using lc.Services.Interfaces;
using System.Globalization;

namespace lc.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppState        _appState;
        private readonly IUserRepository _userRepository;

        public AuthService(AppState appState, IUserRepository userRepository)
        {
            _appState = appState;
            _userRepository = userRepository;
        }

        public async Task<User?> LoginAsync(string userName, string password)
        {
            var user = await _userRepository.AuthenticateAsync(userName, password);
            if (user == null)
                return null;

            _appState.CurrentUser = user;
            await ApplyUserSettingsAsync(user);

            return user;
        }

        public async Task<User> RegisterAsync(string userName, string password)
        {
            var user = await _userRepository.RegisterAsync(userName, password);
            _appState.CurrentUser = user;
            await ApplyUserSettingsAsync(user);
            return user;
        }

        public async Task ApplyUserSettingsAsync(User user)
        {
            _appState.CurrentLanguage = user.PreferredLanguage;


            _appState.CurrentTheme = user.PreferredTheme;

            await Task.CompletedTask;
        }

        public void Logout()
        {
            _appState.CurrentUser = null;
            _appState.SelectedBook = null;

            _appState.CurrentLanguage = Language.Русский;
            _appState.CurrentTheme = "Dark";
        }
    }
}