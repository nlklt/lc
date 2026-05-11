using System.Windows.Input;
using lc.Commands;
using lc.Infrastructure;
using lc.Services.Interfaces;
using lc.ViewModels.Base;

namespace lc.ViewModels
{
    public class NavigationViewModel : ViewModelBase
    {
        private readonly AppState _appState;
        private readonly INavigationService _navigation;
        private readonly IAuthService _authService;

        public bool IsAuthenticated => _appState.IsAuthenticated;
        public bool IsGuest => _appState.IsGuest;
        public bool IsWriter => _appState.IsWriter;
        public bool IsAdmin => _appState.IsAdmin;

        public string CurrentUserName => _appState.CurrentUserName;

        public ICommand NavigateCatalogCommand { get; }
        public ICommand NavigateLoginCommand { get; }
        public ICommand NavigateRegisterCommand { get; }
        public ICommand NavigateEditBookCommand { get; }
        public ICommand NavigateProfileCommand { get; }
        public ICommand LogoutCommand { get; }

        public NavigationViewModel()
        {
            _appState = ServiceLocator.AppState;
            _navigation = ServiceLocator.NavigationService;
            _authService = ServiceLocator.AuthService;

            NavigateCatalogCommand =
                new RelayCommand(_ => _navigation.Navigate(new CatalogViewModel()));

            NavigateLoginCommand =
                new RelayCommand(_ => _navigation.Navigate(new LoginViewModel()));

            NavigateRegisterCommand =
                new RelayCommand(_ => _navigation.Navigate(new RegisterViewModel()));

            NavigateEditBookCommand =
                new RelayCommand(_ => _navigation.Navigate(new EditBookViewModel()));

            NavigateProfileCommand =
                new RelayCommand(_ => _navigation.Navigate(new ProfileViewModel()));

            LogoutCommand =
                new RelayCommand(_ => Logout());

            _appState.PropertyChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(IsAuthenticated));
                OnPropertyChanged(nameof(IsGuest));
                OnPropertyChanged(nameof(IsWriter));
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(CurrentUserName));
            };
        }

        private void Logout()
        {
            _authService.Logout();
            _navigation.Navigate(new CatalogViewModel());
        }
    }
}