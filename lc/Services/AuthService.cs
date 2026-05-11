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
        private readonly IUserRepository _userRepository;
        private readonly AppState _appState;
        //private readonly IThemeService _themeService;
        //private readonly ILanguageService _languageService;

        public AuthService(
            IUserRepository userRepository,
            AppState appState //,
            //IThemeService themeService,
            //ILanguageService languageService
            )
        {
            _userRepository = userRepository;
            _appState = appState;
            //_themeService = themeService;
            //_languageService = languageService;
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
            _appState.CurrentCulture = user.PreferredLanguage switch
            {
                Language.Русский => new CultureInfo("ru-RU"),
                Language.Английский => new CultureInfo("en-US"),
                _ => new CultureInfo("ru-RU")
            };

            CultureInfo.DefaultThreadCurrentCulture = _appState.CurrentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _appState.CurrentCulture;

            _appState.CurrentTheme = user.PreferredTheme;
            //_themeService.ApplyTheme(user.PreferredTheme);
            //_languageService.ApplyLanguage(user.PreferredLanguage);

            await Task.CompletedTask;
        }

        public void Logout()
        {
            _appState.CurrentUser = null;
            _appState.SelectedBook = null;
            _appState.SelectedChapter = null;

            _appState.CurrentCulture = new CultureInfo("ru-RU");
            _appState.CurrentTheme = "Dark";
            //_themeService.ApplyTheme("Dark");
            //_languageService.ApplyLanguage(Language.Русский);
        }
    }
}