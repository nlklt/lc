using lc.Commands;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.ComponentModel;
using System.Data;
using System.Windows.Input;

namespace lc.ViewModels
{
    public class ProfileViewModel : ViewModelBase
    {
        private readonly AppState _appState;
        
        private readonly IUserRepository        _userRepository;
        private readonly INavigationService     _navigation;

        private string      _userName = string.Empty;
        private string?     _avatarPath;
        private bool        _blockedComments;
        private DateTime    _createdAt;
        private UserRole    _role;
        private Language    _preferredLanguage;
        private string      _preferredTheme = "Dark";

        private bool    _isBusy;
        private string  _statusMessage = string.Empty;

        private string      _originalUserName = string.Empty;
        private string?     _originalAvatarPath;
        //private bool        _originalBlockedComments;
        private Language    _originalPreferredLanguage;
        private string      _originalPreferredTheme = "Dark";

        public string UserName
        {
            get => _userName;
            set
            {
                SetProperty(ref _userName, value);
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public string? AvatarPath
        {
            get => _avatarPath;
            set
            {
                SetProperty(ref _avatarPath, value);
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public bool BlockedComments
        {
            get => _blockedComments;
            set
            {
                if (SetProperty(ref _blockedComments, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                }
            }
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
                SetProperty(ref _preferredLanguage, value);
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public string PreferredTheme
        {
            get => _preferredTheme;
            set
            {
                if (SetProperty(ref _preferredTheme, value))
                {
                    ServiceLocator.ThemeService.SetTheme(value);
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        public User? CurrentUser => _appState.CurrentUser;

        // Список доступных языков
        public IEnumerable<Language> AllLanguages => [Language.Русский, Language.Английский];

        // Список доступных тем
        public List<string> AvailableThemes { get; } = new() { "Light", "Dark" };

        public ICommand SaveSettingsCommand { get; }
        public ICommand ResetSettingsCommand { get; }

        public ICommand ChangeAvatarCommand { get; }
        public ICommand ClearAvatarCommand { get; }

        public ICommand LogoutCommand { get; }
        public ICommand DeleteAccountCommand { get; }

        public ProfileViewModel()
        {
            _appState = ServiceLocator.AppState;
            _userRepository = ServiceLocator.UserRepository;

            _appState.PropertyChanged += AppStateOnPropertyChanged;

            LoadFromCurrentUser();

            SaveSettingsCommand     = new AsyncRelayCommand(SaveSettingsAsync, CanSaveSettings);
            ResetSettingsCommand    = new RelayCommand(_ => LoadFromCurrentUser(), _ 
                =>  !IsBusy);
            
            ChangeAvatarCommand     = new RelayCommand(OnChangeAvatar);
            ClearAvatarCommand      = new RelayCommand(_ => AvatarPath = null, _ 
                =>  !IsBusy && !string.IsNullOrWhiteSpace(AvatarPath));
            
            LogoutCommand           = new RelayCommand(OnLogout);
            DeleteAccountCommand    = new AsyncRelayCommand(DeleteAccountAsync);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool CanSave => !IsBusy && HasChanges() && !string.IsNullOrWhiteSpace(UserName);

        private void AppStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppState.CurrentUser))
            {
                LoadFromCurrentUser();
            }
        }

        private void LoadFromCurrentUser()
        {
            var user = _appState.CurrentUser;

            if (user == null)
            {
                UserName            = string.Empty;
                AvatarPath          = null;
                BlockedComments     = false;
                CreatedAt           = default;
                Role                = default;
                PreferredLanguage   = _appState.CurrentLanguage;
                PreferredTheme      = _appState.CurrentTheme;

                StoreOriginalValues();
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
            return !string.Equals(UserName?.Trim(), _originalUserName?.Trim(), StringComparison.Ordinal) ||
                   !string.Equals(AvatarPath?.Trim(), _originalAvatarPath?.Trim(), StringComparison.Ordinal) ||
                   PreferredLanguage != _originalPreferredLanguage ||
                   !string.Equals(PreferredTheme?.Trim(), _originalPreferredTheme?.Trim(), StringComparison.Ordinal);
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = string.Empty;

                var user = _appState.CurrentUser;

                user.UserName = UserName.Trim();
                user.AvatarPath = string.IsNullOrWhiteSpace(AvatarPath) ? null : AvatarPath.Trim();
                user.BlockedComments = BlockedComments;
                user.PreferredLanguage = PreferredLanguage;
                user.PreferredTheme = string.IsNullOrWhiteSpace(PreferredTheme) ? "Dark" : PreferredTheme.Trim();

                await _userRepository.UpdateAsync(user);

                _appState.CurrentUser = user;
                _appState.CurrentLanguage = user.PreferredLanguage;
                _appState.CurrentTheme = user.PreferredTheme;

                StoreOriginalValues();

                StatusMessage = "Настройки сохранены.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения профиля: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanSaveSettings()
        {
            if (_appState.CurrentUser == null)
            {
                StatusMessage = "Пользователь не авторизован.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(UserName))
            {
                StatusMessage = "Имя пользователя не может быть пустым.";
                return false;
            }
            return true;
        }

        private void OnChangeAvatar(object? parameter)
        {
            var path = ServiceLocator.DialogService.OpenFile("Выберите изображение", "Image Files|*.jpg;*.jpeg;*.png");
            if (!string.IsNullOrEmpty(path))
            {
                AvatarPath = path;
            }
        }

        private async Task DeleteAccountAsync()
        {
            _userRepository.DeleteAsync(CurrentUser.UserId);
            _navigation.Navigate(new CatalogViewModel());
        }

        private void OnLogout(object? parameter)
        {
            ServiceLocator.AuthService.Logout();
            ServiceLocator.NavigationService.Navigate(new CatalogViewModel());
        }
    }
}