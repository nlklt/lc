using System.Windows.Input;
using lc.Models;
using lc.Models.Enums;
using lc.Infrastructure;
using lc.ViewModels.Base;
using lc.Commands;

namespace lc.ViewModels
{
    public class ProfileViewModel : ViewModelBase
    {
        private readonly AppState _appState;
        private string? _avatarPath;
        private string _userName;
        private Language _selectedLanguage;
        private string _selectedTheme;

        public ProfileViewModel()
        {
            _appState = ServiceLocator.AppState;

            if (_appState.CurrentUser != null)
            {
                _userName = _appState.CurrentUser.UserName;
                _avatarPath = _appState.CurrentUser.AvatarPath;
                _selectedLanguage = _appState.CurrentUser.PreferredLanguage;
                _selectedTheme = _appState.CurrentUser.PreferredTheme;
            }
            else
            {
                _userName = "Гость";
                _selectedTheme = "Dark";
            }

            LogoutCommand       = new RelayCommand(OnLogout);
            SaveSettingsCommand = new RelayCommand(OnSaveSettings, CanSaveSettings);
            ChangeAvatarCommand = new RelayCommand(OnChangeAvatar);
        }

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string? AvatarPath
        {
            get => _avatarPath;
            set => SetProperty(ref _avatarPath, value);
        }

        public Language SelectedLanguage
        {
            get => _selectedLanguage;
            set => SetProperty(ref _selectedLanguage, value);
        }

        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (SetProperty(ref _selectedTheme, value))
                {
                    // Мгновенно применяем тему через сервис для предпросмотра
                    ServiceLocator.ThemeService.SetTheme(value);
                }
            }
        }

        public User? CurrentUser => _appState.CurrentUser;

        // Список всех доступных языков для ComboBox в View
        public IEnumerable<Language> AllLanguages => Enum.GetValues(typeof(Language)).Cast<Language>();

        // Список доступных тем
        public List<string> AvailableThemes { get; } = new() { "Light", "Dark" };

        public ICommand SaveSettingsCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ChangeAvatarCommand { get; }

        private bool CanSaveSettings(object? parameter) => _appState.IsAuthenticated;

        private async void OnSaveSettings(object? parameter)
        {
            if (_appState.CurrentUser == null) return;

            // Обновляем модель пользователя
            _appState.CurrentUser.UserName = UserName;
            _appState.CurrentUser.AvatarPath = AvatarPath;
            _appState.CurrentUser.PreferredLanguage = SelectedLanguage;
            _appState.CurrentUser.PreferredTheme = SelectedTheme;

            // Сохраняем в базу данных через репозиторий
            bool success = await Task.Run(() => ServiceLocator.UserRepository.UpdateAsync(_appState.CurrentUser));

            if (success)
            {
                _appState.CurrentLanguage = SelectedLanguage;
                _appState.CurrentTheme = SelectedTheme;

                ServiceLocator.DialogService.ShowMessageAsync("Успех", "Настройки успешно сохранены!");
            }
            else
            {
                ServiceLocator.DialogService.ShowMessageAsync("Ошибка", "Ошибка при сохранении настроек.");
            }
        }

        private void OnChangeAvatar(object? parameter)
        {
            var path = ServiceLocator.DialogService.OpenFile("Выберите изображение", "Image Files|*.jpg;*.jpeg;*.png");
            if (!string.IsNullOrEmpty(path))
            {
                AvatarPath = path;
            }
        }

        private void OnLogout(object? parameter)
        {
            ServiceLocator.AuthService.Logout();
            ServiceLocator.NavigationService.Navigate(new CatalogViewModel());
        }
    }
}