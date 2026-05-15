using lc.Commands;
using lc.Helpers;
using lc.Infrastructure;
using lc.Models;
using lc.Models.Enums;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.ComponentModel;
using System.Windows.Input;

namespace lc.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AppState           _appState;
        private readonly IAuthService       _auth;
        private readonly INavigationService _navigation;

        public NavigationViewModel Navigation { get; }

        public ViewModelBase?   CurrentViewModel => _appState.CurrentViewModel;
        public bool IsGuest =>  _appState.IsGuest;
        public bool IsReader => _appState.IsReader;
        public bool IsWriter => _appState.IsWriter;
        public bool IsAdmin =>  _appState.IsAdmin;
        public bool IsAuthenticated =>      _appState.IsAuthenticated;
        public string CurrentUserName =>    _appState.CurrentUser?.UserName ?? "Гость";
        public UserRole CurrentUserRole =>  _appState.CurrentUser?.Role ?? UserRole.Guest;

        public ICommand LogoutCommand { get; }

        public MainViewModel(
            AppState appState,
            IAuthService auth,
            INavigationService navigation,
            NavigationViewModel navigationViewModel)
        {
            _appState   = appState      ?? throw new ArgumentNullException(nameof(appState));
            _auth       = auth          ?? throw new ArgumentNullException(nameof(auth));
            _navigation = navigation    ?? throw new ArgumentNullException(nameof(navigation));

            Navigation = navigationViewModel ?? throw new ArgumentNullException(nameof(navigationViewModel));

            LogoutCommand = new RelayCommand(_ => Logout(), _ => IsAuthenticated);

            _appState.PropertyChanged += AppStateOnPropertyChanged;

            Initialize();
        }

        private void Initialize()
        {
            if (IsAuthenticated) _navigation.NavigateTo<ProfileViewModel>();
            else _navigation.NavigateTo<CatalogViewModel>();
        }

        private void Logout()
        {
            _auth.Logout();
            _navigation.NavigateTo<CatalogViewModel>();
        }

        private void AppStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
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
        }
    }
}
