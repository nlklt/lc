using lc.Commands;
using lc.Infrastructure;
using lc.Models;
using lc.Models.Enums;
using lc.Services;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Windows.Input;

namespace lc.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AppState           _appState;
        private readonly IAuthService       _auth;
        private readonly INavigationService _navigation;

        public ViewModelBase CurrentViewModel => _appState.CurrentViewModel ?? new CatalogViewModel();
        public bool IsGuest => _appState.IsGuest;
        public bool IsReader => _appState.IsReader;
        public bool IsWriter => _appState.IsWriter;
        public bool IsAdmin => _appState.IsAdmin;
        public bool IsAuthenticated => _appState.IsAuthenticated;
        public string CurrentUserName => _appState.CurrentUser?.UserName ?? "Гость";
        public UserRole CurrentUserRole => _appState.CurrentUser?.Role ?? UserRole.Guest;

        public ICommand LogoutCommand { get; }

        public MainViewModel()
        {
            _appState   = ServiceLocator.AppState;

            User newUser = new();
                newUser.UserId = 999;
                newUser.UserName = "test_admin";
                newUser.PasswordHash = PasswordHasher.Hash("flvby1234");
                newUser.AvatarPath = "";
                newUser.BlockedComments = false;
                newUser.CreatedAt = DateTime.Now;
                newUser.Role = (UserRole)3;
                newUser.PreferredLanguage = (Language)0;
                newUser.PreferredTheme = "Dark";

            _appState.CurrentUser = newUser;

            _auth       = ServiceLocator.AuthService;
            _navigation = ServiceLocator.NavigationService;

            LogoutCommand = new RelayCommand(_ => Logout(), _ => IsAuthenticated);

            _appState.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppState.CurrentViewModel))
                    OnPropertyChanged(nameof(CurrentViewModel));

                if (e.PropertyName == nameof(AppState.CurrentUser))
                {
                    OnPropertyChanged(nameof(IsGuest));
                    OnPropertyChanged(nameof(IsReader));
                    OnPropertyChanged(nameof(IsWriter));
                    OnPropertyChanged(nameof(IsAdmin));
                    OnPropertyChanged(nameof(IsAuthenticated));
                    OnPropertyChanged(nameof(CurrentUserName));
                    OnPropertyChanged(nameof(CurrentUserRole));
                }
            };

            Initialize();
        }

        private void Initialize()
        {
            if (_appState.CurrentUser == null)
                _navigation.Navigate(new CatalogViewModel());
            else
                _navigation.Navigate(new ProfileViewModel());
        }

        private void Logout()
        {
            _auth.Logout();
            _navigation.Navigate(new CatalogViewModel());
        }
    }
}
