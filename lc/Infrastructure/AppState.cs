using lc.Models;
using lc.Models.Enums;
using lc.ViewModels.Base;
using System.Formats.Asn1;
using System.Globalization;

namespace lc.Infrastructure
{
    public class AppState : ObservableObject
    {
        private ViewModelBase _prevViewModel = null!;
        private ViewModelBase _currentViewModel = null!;
        private User? _currentUser;
        private Chapter? _selectedChapter;
        private BookListItem? _selectedBook;
        private CultureInfo _currentCulture = new CultureInfo("ru-RU");
        private string _currentTheme = "Dark";

        public ViewModelBase PrevViewModel
        {
            get => _prevViewModel;
            set =>  SetProperty(ref _prevViewModel, value);
        }
        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _prevViewModel = _currentViewModel;
                SetProperty(ref _currentViewModel, value);
            }
        }
        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (SetProperty(ref _currentUser, value))
                {
                    OnPropertyChanged(nameof(IsAuthenticated));
                    OnPropertyChanged(nameof(IsGuest));
                    OnPropertyChanged(nameof(IsAdmin));
                    OnPropertyChanged(nameof(IsWriter));
                    OnPropertyChanged(nameof(IsReader));
                    OnPropertyChanged(nameof(CanManageBooks));
                    OnPropertyChanged(nameof(CanComment));
                    OnPropertyChanged(nameof(CurrentUserName));
                    OnPropertyChanged(nameof(CurrentRoleName));
                }
            }
        }
        public bool IsAuthenticated => CurrentUser != null;
        public bool IsGuest => CurrentUser == null;
        public bool IsAdmin => CurrentUser?.Role == UserRole.Admin;
        public bool IsWriter => CurrentUser?.Role == UserRole.Writer || IsAdmin;
        public bool IsReader => CurrentUser?.Role == UserRole.Reader || IsAdmin || IsWriter;

        public bool CanManageBooks => IsAdmin || IsWriter;
        public bool CanComment => IsAuthenticated && !(CurrentUser?.BlockedComments ?? false);

        public string CurrentUserName => CurrentUser?.UserName ?? "Гость";
        public string CurrentRoleName => CurrentUser?.Role.ToString();

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set => SetProperty(ref _currentCulture, value);
        }

        public string CurrentTheme
        {
            get => _currentTheme;
            set => SetProperty(ref _currentTheme, value);
        }

        public BookListItem? SelectedBook
        {
            get => _selectedBook;
            set => SetProperty(ref _selectedBook, value);
        }

        public Chapter? SelectedChapter
        {
            get => _selectedChapter;
            set => SetProperty(ref _selectedChapter, value);
        }
    }
}