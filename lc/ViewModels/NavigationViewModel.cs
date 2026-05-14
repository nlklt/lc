using lc.Commands;
using lc.Infrastructure;
using lc.Models.Enums;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System;
using System.Windows.Input;

namespace lc.ViewModels
{
    public class NavigationViewModel : ViewModelBase
    {
        private readonly AppState _appState;

        private readonly IAuthService       _auth;
        private readonly IDialogService     _dialog;
        private readonly INavigationService _navigation;

        private readonly IThemeService        _themeService;
        private readonly ILocalizationService _localizationService;

        public bool IsGuest => _appState.IsGuest;
        public bool IsWriter => _appState.IsWriter;
        public bool IsAdmin => _appState.IsAdmin;
        public bool IsReader => _appState.IsReader;
        public bool IsAuthenticated => _appState.IsAuthenticated;

        public string CurrentUserName => _appState.CurrentUser?.UserName ?? "Гость";
        public UserRole CurrentUserRole => _appState.CurrentUser?.Role ?? UserRole.Guest;
        public string AvatarPath => _appState.CurrentUser?.AvatarPath ?? "";

        // Гость
        public ICommand NavigateRegisterCommand { get; }
        public ICommand NavigateLoginCommand { get; }

        // Читатель
        public ICommand NavigateCatalogCommand { get; }
        public ICommand RandomBookCommand { get; }
        public ICommand NavigateMarkBookCommand { get; }
        public ICommand BecomeAuthorCommand { get; }


        public ICommand NavigateCreateBookCommand { get; }
        public ICommand NavigateProfileCommand { get; }

        public ICommand NavigateSettingsCommand { get; }
        public ICommand LogoutCommand { get; }

        public NavigationViewModel()
        {
            _appState = ServiceLocator.AppState;

            _auth       = ServiceLocator.AuthService;
            _dialog     = ServiceLocator.DialogService;
            _navigation = ServiceLocator.NavigationService;

            _themeService = ServiceLocator.ThemeService;
            _localizationService = ServiceLocator.LocalisationService;

            NavigateLoginCommand    = new RelayCommand(_ => _dialog.ShowLoginDialog());
            NavigateRegisterCommand = new RelayCommand(_ => _dialog.ShowRegisterDialog());
            
            NavigateProfileCommand  = new RelayCommand(_ => _navigation.Navigate(new ProfileViewModel()));

            NavigateCatalogCommand  = new RelayCommand(_ => _navigation.Navigate(new CatalogViewModel()));

            RandomBookCommand = new RelayCommand(_ =>
            {
                Random random = new();
                //var books = await ServiceLocator.BookService.GetRandomBookAsync();
                int bookId = random.Next(21, 32);
                _navigation.Navigate(new BookDetailsViewModel(bookId));
            });

            // NavigateMarkBookCommand = new RelayCommand(_ => _navigation.Navigate(new BookDetailsViewModel()));

            // BecomeAuthorCommand = new RelayCommand(_ => _navigation.Navigate(new EditBookViewModel()));

            NavigateCreateBookCommand = new RelayCommand(_ => _navigation.Navigate(new EditBookViewModel()));

            NavigateSettingsCommand = new RelayCommand(_ =>
            {
                var vm = new ProfileViewModel();
                vm.IsSettingsOpen = true;
                _navigation.Navigate(vm);
            });

            LogoutCommand           = new RelayCommand(_ => Logout());

            _appState.PropertyChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(IsGuest));
                OnPropertyChanged(nameof(IsWriter));
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(IsAuthenticated));
                OnPropertyChanged(nameof(CurrentUserName));
                OnPropertyChanged(nameof(CurrentUserRole));
                OnPropertyChanged(nameof(AvatarPath));
            };
        }

        private void Logout()
        {
            _auth.Logout();
            _navigation.Navigate(new CatalogViewModel());
        }
    }
}