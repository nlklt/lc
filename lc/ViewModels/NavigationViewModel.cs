using lc.Commands;
using lc.Infrastructure;
using lc.Models.Enums;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Windows.Input;

namespace lc.ViewModels
{
    public class NavigationViewModel : ViewModelBase
    {
        private readonly AppState _appState;

        private readonly IAuthService       _auth;
        private readonly IDialogService     _dialog;
        private readonly INavigationService _navigation;

        public bool IsGuest => _appState.IsGuest;
        public bool IsWriter => _appState.IsWriter;
        public bool IsAdmin => _appState.IsAdmin;
        public bool IsAuthenticated => _appState.IsAuthenticated;

        public string CurrentUserName => _appState.CurrentUser?.UserName ?? "Гость";
        public UserRole CurrentUserRole => _appState.CurrentUser?.Role ?? UserRole.Guest;

        public ICommand NavigateCatalogCommand { get; }
        public ICommand NavigateLoginCommand { get; }
        public ICommand NavigateRegisterCommand { get; }
        public ICommand NavigateEditBookCommand { get; }
        public ICommand NavigateProfileCommand { get; }
        public ICommand LogoutCommand { get; }

        public NavigationViewModel()
        {
            _appState = ServiceLocator.AppState;

            _auth       = ServiceLocator.AuthService;
            _dialog     = ServiceLocator.DialogService;
            _navigation = ServiceLocator.NavigationService;

            NavigateLoginCommand    = new RelayCommand(_ => _dialog.ShowLoginDialog());
            NavigateRegisterCommand = new RelayCommand(_ => _dialog.ShowRegisterDialog());
            NavigateCatalogCommand  = new RelayCommand(_ => _navigation.Navigate(new CatalogViewModel()));
            NavigateProfileCommand  = new RelayCommand(_ => _navigation.Navigate(new ProfileViewModel()));
            NavigateEditBookCommand = new RelayCommand(_ => _navigation.Navigate(new EditBookViewModel()));
            LogoutCommand           = new RelayCommand(_ => Logout());

            _appState.PropertyChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(IsGuest));
                OnPropertyChanged(nameof(IsWriter));
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(IsAuthenticated));
                OnPropertyChanged(nameof(CurrentUserName));
                OnPropertyChanged(nameof(CurrentUserRole));
            };
        }

        private void Logout()
        {
            _auth.Logout();
            _navigation.Navigate(new CatalogViewModel());
        }
    }
}