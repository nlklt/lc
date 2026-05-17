using lc.Helpers;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using lc.Services.Interfaces;

namespace lc.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppState _appState;
    private readonly IUserRepository _userRepository;

    public AuthService(AppState appState, IUserRepository userRepository)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<User?> LoginAsync(string userName, string password)
    {
        var user = await _userRepository.GetByUserNameAsync(userName);
        if (user is null)
            return null;

        if (!PasswordHasher.Verify(password, user.PasswordHash))
            return null;

        _appState.CurrentUser = user;

        return user;
    }

    public async Task<User> RegisterAsync(string userName, string password)
    {
        var existing = await _userRepository.GetByUserNameAsync(userName);
        if (existing is not null)
            throw new InvalidOperationException("Пользователь уже существует.");

        var user = new User
        {
            UserName = userName,
            PasswordHash = PasswordHasher.Hash(password),
            CreatedAt = DateTime.Now,
            Role = UserRole.Reader,
            PreferredLanguage = Language.Русский,
            PreferredTheme = "Dark",
            BlockedComments = false
        };

        await _userRepository.CreateAsync(user);
        _appState.CurrentUser = user;

        return user;
    }

    public Task ApplyUserSettingsAsync(User user)
    {
        _appState.CurrentUser = null;
        _appState.CurrentLanguage = Language.Русский;
        _appState.CurrentTheme = "Dark";
        _appState.RefreshCurrentUser();
        return Task.CompletedTask;
    }

    public void Logout()
    {
        _appState.SelectedBook = null;
        _appState.CurrentUser = null;
        _appState.CurrentLanguage = Language.Русский;
        _appState.CurrentTheme = "Dark";
        _appState.RefreshCurrentUser();
    }
}