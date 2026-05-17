using lc.Commands;
using lc.Helpers;
using lc.Infrastructure;
using lc.Models.Enums;
using lc.Services;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace lc.ViewModels
{
    public class NavigationViewModel : ViewModelBase
    {
        private readonly AppState               _appState;
        private readonly IAuthService           _auth;
        private readonly IDialogService         _dialog;
        private readonly INavigationService     _navigation;
        private readonly IBookService           _bookService;

        public bool IsGuest =>          _appState.IsGuest;
        public bool IsWriter =>         _appState.IsWriter;
        public bool IsAdmin =>          _appState.IsAdmin;
        public bool IsReader =>         _appState.IsReader;
        public bool IsAuthenticated =>  _appState.IsAuthenticated;

        public bool CanRequestAuthorRole =>  _appState.CanRequestAuthorRole;

        public string AvatarPath =>         _appState.CurrentUser?.AvatarPath ?? string.Empty;
        public string CurrentUserName =>    _appState.CurrentUser?.UserName ?? "Гость";
        public UserRole CurrentUserRole =>  _appState.CurrentUser?.Role ?? UserRole.Guest;

        // Гость 4/4
        public ICommand NavigateRegisterCommand { get; }
        public ICommand NavigateLoginCommand { get; }
        public ICommand NavigateCatalogCommand { get; }
        public ICommand RandomBookCommand { get; }

        // Читатель 3/4
        public ICommand NavigateProfileCommand { get; }
        public ICommand BecomeAuthorCommand { get; }
        public ICommand NavigateSettingsCommand { get; }
        public ICommand LogoutCommand { get; }

        // Писатель 1/1 + 2
        public ICommand NavigateCreateBookCommand { get; }

        // Админ 0/0 + 2


        public NavigationViewModel(
            AppState appState,
            IAuthService auth,
            IDialogService dialog,
            INavigationService navigation,
            IBookService bookService)
        {
            _appState               = appState ?? throw new ArgumentNullException(nameof(appState));
            _auth                   = auth ?? throw new ArgumentNullException(nameof(auth));
            _dialog                 = dialog ?? throw new ArgumentNullException(nameof(dialog));
            _navigation             = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _bookService            = bookService ?? throw new ArgumentNullException(nameof(bookService));

            NavigateLoginCommand    = new RelayCommand(_ => _dialog.ShowLoginDialog());
            NavigateRegisterCommand = new RelayCommand(_ => _dialog.ShowRegisterDialog());
            NavigateCatalogCommand  = new RelayCommand(_ => _navigation.NavigateTo<CatalogViewModel>());
            RandomBookCommand       = new AsyncRelayCommand(NavigateRandomBookAsync);

            NavigateProfileCommand      = new RelayCommand(_ => _navigation.NavigateTo<ProfileViewModel>());
            NavigateSettingsCommand     = new RelayCommand(_ => _navigation.NavigateTo <ProfileViewModel>(true));
            LogoutCommand               = new RelayCommand(_ => Logout());

            NavigateCreateBookCommand = new RelayCommand(_ =>
            {
                if (!IsWriter && !IsAdmin)
                {
                    _ = _dialog.ShowMessageAsync("Недоступно", "Создание книги доступно только авторам.");
                    return;
                }

                _navigation.NavigateTo<EditBookViewModel>();
            });

            _appState.PropertyChanged += AppStateOnPropertyChanged;
        }

        private async Task NavigateRandomBookAsync()
        {
            var criteria = new BookFilterCriteria
            {
                IncludeBookStatuses = [BookStatus.Published]
            };

            var books = await _bookService.GetCatalogAsync(criteria);

            if (books.Count == 0)
            {
                await _dialog.ShowMessageAsync("Книги не найдены", "В каталоге нет опубликованных книг.");
                return;
            }

            var random = Random.Shared.Next(books.Count);
            var bookId = books[random].BookId;

            _navigation.NavigateTo<BookDetailsViewModel>(bookId);
        }

        private void Logout()
        {
            _auth.Logout();
            _navigation.NavigateTo<CatalogViewModel>();
        }

        private void AppStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(AppState.CurrentUser))
            {
                OnPropertyChanged(nameof(IsGuest));
                OnPropertyChanged(nameof(IsWriter));
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(IsReader));
                OnPropertyChanged(nameof(IsAuthenticated));

                OnPropertyChanged(nameof(CanRequestAuthorRole));

                OnPropertyChanged(nameof(AvatarPath));
                OnPropertyChanged(nameof(CurrentUserName));
                OnPropertyChanged(nameof(CurrentUserRole));
            }
        }
    }
}