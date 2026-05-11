using lc.Commands;
using lc.Infrastructure;
using lc.Models.Enums;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace lc.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AppState _appState;
        private readonly INavigationService _navigation;
        private readonly IAuthService _authService;

        public ViewModelBase CurrentViewModel => _appState.CurrentViewModel;
        public bool IsAuthenticated => _appState.IsAuthenticated;
        public bool IsAdmin => _appState.IsAdmin;
        public bool IsWriter => _appState.IsWriter;
        public bool IsReader => _appState.IsReader;
        public bool IsGuest => _appState.IsGuest;
        public string CurrentUserName => _appState.CurrentUserName;
        public string CurrentRoleName => _appState.CurrentRoleName;

        public ICommand LogoutCommand { get; }

        public MainViewModel()
        {
            _appState = ServiceLocator.AppState;
            _navigation = ServiceLocator.NavigationService;
            _authService = ServiceLocator.AuthService;

            LogoutCommand = new RelayCommand(_ => Logout(), _ => IsAuthenticated);

            _appState.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppState.CurrentViewModel))
                    OnPropertyChanged(nameof(CurrentViewModel));

                if (e.PropertyName == nameof(AppState.CurrentUser))
                {
                    OnPropertyChanged(nameof(IsAuthenticated));
                    OnPropertyChanged(nameof(IsAdmin));
                    OnPropertyChanged(nameof(IsWriter));
                    OnPropertyChanged(nameof(IsReader));
                    OnPropertyChanged(nameof(IsGuest));
                    OnPropertyChanged(nameof(CurrentUserName));
                    OnPropertyChanged(nameof(CurrentRoleName));
                }
            };

            Initialize();
        }
        private void Initialize()
        {
            _navigation.Navigate(new CatalogViewModel());
        }

        private void Logout()
        {
            _authService.Logout();
            _navigation.Navigate(new CatalogViewModel());
        }
    }
}
