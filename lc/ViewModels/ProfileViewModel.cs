using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using lc.Commands;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using Microsoft.Win32;

namespace lc.ViewModels;

public sealed class ProfileViewModel : ViewModelBase, IDisposable
{
    private const int MaxUserNameLength = 16;
    private const int MinUserNameLength = 3;

    private readonly AppState _appState;
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly IDialogService _dialogService;

    private bool _isBusy;
    private bool _isSettingsOpen;
    private bool _isDisposed;
    private string _statusMessage = string.Empty;

    private string _userName = string.Empty;
    private string? _avatarPath;
    private bool _blockedComments;
    private DateTime _createdAt;
    private UserRole _role;
    private Language _preferredLanguage = Language.Русский;
    private string _preferredTheme = "Dark";

    private string _originalUserName = string.Empty;
    private string? _originalAvatarPath;
    private Language _originalPreferredLanguage;
    private string _originalPreferredTheme = "Dark";

    public ProfileViewModel(
        AppState appState,
        IUserRepository userRepository,
        IAuthService authService,
        INavigationService navigationService,
        IThemeService themeService,
        ILocalizationService localizationService,
        IDialogService dialogService)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        _appState.PropertyChanged += OnAppStatePropertyChanged;

        SaveSettingsCommand = new AsyncRelayCommand(_ => SaveSettingsAsync(), _ => CanSave);
        ResetSettingsCommand = new RelayCommand(_ => LoadFromCurrentUser(), _ => !IsBusy);
        ChangeAvatarCommand = new AsyncRelayCommand(_ => ChangeAvatarAsync(), _ => !IsBusy && IsAuthenticated);
        ClearAvatarCommand = new RelayCommand(_ => AvatarPath = null, _ => !IsBusy && !string.IsNullOrWhiteSpace(AvatarPath));
        LogoutCommand = new AsyncRelayCommand(_ => LogoutAsync(), _ => IsAuthenticated && !IsBusy);
        DeleteAccountCommand = new AsyncRelayCommand(_ => DeleteAccountAsync(), _ => IsAuthenticated && !IsBusy);
        ToggleSettingsCommand = new RelayCommand(_ => IsSettingsOpen = true);
        GoBackCommand = new RelayCommand(_ => IsSettingsOpen = false, _ => IsSettingsOpen);

        LoadFromCurrentUser();
    }

    public User? CurrentUser => _appState.CurrentUser;

    public bool IsAuthenticated => _appState.IsAuthenticated;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RefreshCommands();
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set => SetProperty(ref _isSettingsOpen, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string UserName
    {
        get => _userName;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (SetProperty(ref _userName, normalized))
            {
                RefreshCommands();
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public string? AvatarPath
    {
        get => _avatarPath;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (SetProperty(ref _avatarPath, normalized))
            {
                RefreshCommands();
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public bool BlockedComments
    {
        get => _blockedComments;
        private set => SetProperty(ref _blockedComments, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        private set => SetProperty(ref _createdAt, value);
    }

    public UserRole Role
    {
        get => _role;
        private set => SetProperty(ref _role, value);
    }

    public Language PreferredLanguage
    {
        get => _preferredLanguage;
        set
        {
            if (SetProperty(ref _preferredLanguage, value))
            {
                ApplyLanguagePreview(value);
                RefreshCommands();
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public string PreferredTheme
    {
        get => _preferredTheme;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "Dark" : value.Trim();
            if (SetProperty(ref _preferredTheme, normalized))
            {
                ApplyThemePreview(normalized);
                RefreshCommands();
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public List<string> AvailableThemes { get; } = ["Light", "Dark"];
    public IEnumerable<Language> AllLanguages => [Language.Русский, Language.Английский];

    public bool CanSave =>
        IsAuthenticated &&
        !IsBusy &&
        HasChanges() &&
        IsValid();

    public ICommand SaveSettingsCommand { get; }
    public ICommand ResetSettingsCommand { get; }
    public ICommand ChangeAvatarCommand { get; }
    public ICommand ClearAvatarCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand DeleteAccountCommand { get; }
    public ICommand ToggleSettingsCommand { get; }
    public ICommand GoBackCommand { get; }

    private void OnAppStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppState.CurrentUser))
            LoadFromCurrentUser();
    }

    private void LoadFromCurrentUser()
    {
        var user = _appState.CurrentUser;

        if (user is null)
        {
            UserName = string.Empty;
            AvatarPath = null;
            BlockedComments = false;
            CreatedAt = default;
            Role = default;
            PreferredLanguage = _appState.CurrentLanguage;
            PreferredTheme = _appState.CurrentTheme;

            StoreOriginalValues();
            StatusMessage = string.Empty;
            RefreshCommands();
            OnPropertyChanged(nameof(CanSave));
            return;
        }

        UserName = user.UserName;
        AvatarPath = user.AvatarPath;
        BlockedComments = user.BlockedComments;
        CreatedAt = user.CreatedAt;
        Role = user.Role;
        PreferredLanguage = user.PreferredLanguage;
        PreferredTheme = string.IsNullOrWhiteSpace(user.PreferredTheme) ? "Dark" : user.PreferredTheme;

        StoreOriginalValues();
        StatusMessage = string.Empty;
        RefreshCommands();
        OnPropertyChanged(nameof(CanSave));
    }

    private void StoreOriginalValues()
    {
        _originalUserName = UserName;
        _originalAvatarPath = AvatarPath;
        _originalPreferredLanguage = PreferredLanguage;
        _originalPreferredTheme = PreferredTheme;
    }

    private bool HasChanges()
    {
        return !string.Equals(UserName.Trim(), _originalUserName.Trim(), StringComparison.Ordinal) ||
               !string.Equals(Normalize(AvatarPath), Normalize(_originalAvatarPath), StringComparison.Ordinal) ||
               PreferredLanguage != _originalPreferredLanguage ||
               !string.Equals(PreferredTheme.Trim(), _originalPreferredTheme.Trim(), StringComparison.Ordinal);
    }

    private bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(UserName))
            return false;

        if (UserName.Length < MinUserNameLength || UserName.Length > MaxUserNameLength)
            return false;

        if (!string.IsNullOrWhiteSpace(AvatarPath) && !File.Exists(AvatarPath))
            return false;

        if (!AvailableThemes.Contains(PreferredTheme))
            return false;

        return PreferredLanguage is Language.Русский or Language.Английский;
    }

    private async Task SaveSettingsAsync()
    {
        if (!IsAuthenticated || _appState.CurrentUser is null)
        {
            StatusMessage = "Пользователь не авторизован.";
            return;
        }

        if (!IsValid())
        {
            StatusMessage = "Проверьте введённые данные.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            var user = _appState.CurrentUser;

            var normalizedUserName = UserName.Trim();
            var normalizedAvatar = string.IsNullOrWhiteSpace(AvatarPath) ? null : AvatarPath.Trim();
            var normalizedTheme = string.IsNullOrWhiteSpace(PreferredTheme) ? "Dark" : PreferredTheme.Trim();

            if (!string.Equals(user.UserName, normalizedUserName, StringComparison.Ordinal))
            {
                var exists = await _userRepository.ExistsByUserNameAsync(normalizedUserName);
                if (exists)
                {
                    StatusMessage = "Пользователь с таким именем уже существует.";
                    return;
                }
            }

            user.UserName = normalizedUserName;
            user.AvatarPath = normalizedAvatar;
            user.PreferredLanguage = PreferredLanguage;
            user.PreferredTheme = normalizedTheme;

            var updated = await _userRepository.UpdateAsync(user);
            if (!updated)
            {
                StatusMessage = "Не удалось сохранить профиль.";
                return;
            }

            _appState.CurrentUser = user;
            _appState.CurrentLanguage = user.PreferredLanguage;
            _appState.CurrentTheme = user.PreferredTheme;

            StoreOriginalValues();
            StatusMessage = "Настройки сохранены.";
        }
        catch
        {
            StatusMessage = "Ошибка сохранения профиля.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ChangeAvatarAsync()
    {
        if (IsBusy || !IsAuthenticated)
            return;

        var path = _dialogService.OpenFile(
            "Выберите изображение",
            "Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp");

        if (string.IsNullOrWhiteSpace(path))
            return;

        AvatarPath = path;
        await Task.CompletedTask;
    }

    private async Task LogoutAsync()
    {
        if (!IsAuthenticated)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Выход",
            "Выйти из аккаунта?");

        if (!confirmed)
            return;

        _authService.Logout();
        _navigationService.NavigateTo<CatalogViewModel>();
    }

    private async Task DeleteAccountAsync()
    {
        if (!IsAuthenticated || _appState.CurrentUser is null)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Удаление аккаунта",
            "Аккаунт будет удалён без возможности восстановления. Продолжить?");

        if (!confirmed)
            return;

        try
        {
            IsBusy = true;

            var userId = _appState.CurrentUser.UserId;

            await _userRepository.DeleteAsync(userId);
            _authService.Logout();
            _navigationService.NavigateTo<CatalogViewModel>();
        }
        catch
        {
            StatusMessage = "Не удалось удалить аккаунт.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyThemePreview(string themeName)
    {
        if (!string.Equals(_appState.CurrentTheme, themeName, StringComparison.Ordinal))
            _themeService.SetTheme(themeName);
    }

    private void ApplyLanguagePreview(Language language)
    {
        var code = language switch
        {
            Language.Русский => "ru",
            Language.Английский => "en",
            _ => "ru"
        };

        _localizationService.SetLanguage(code);
    }

    private void RefreshCommands()
    {
        if (SaveSettingsCommand is AsyncRelayCommand save)
            save.RaiseCanExecuteChanged();

        if (ResetSettingsCommand is RelayCommand reset)
            reset.RaiseCanExecuteChanged();

        if (ChangeAvatarCommand is AsyncRelayCommand changeAvatar)
            changeAvatar.RaiseCanExecuteChanged();

        if (ClearAvatarCommand is RelayCommand clearAvatar)
            clearAvatar.RaiseCanExecuteChanged();

        if (LogoutCommand is AsyncRelayCommand logout)
            logout.RaiseCanExecuteChanged();

        if (DeleteAccountCommand is AsyncRelayCommand delete)
            delete.RaiseCanExecuteChanged();

        if (GoBackCommand is RelayCommand goBack)
            goBack.RaiseCanExecuteChanged();
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _appState.PropertyChanged -= OnAppStatePropertyChanged;
    }
}