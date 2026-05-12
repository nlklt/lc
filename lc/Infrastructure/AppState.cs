using lc.Models;
using lc.Models.Enums;
using lc.ViewModels.Base;

namespace lc.Infrastructure
{
    public class AppState : ObservableObject
    {
        private User? _currentUser;
        private string _currentTheme = "Dark";
        private Language _currentLanguage = Language.Русский;
        
        private Book? _selectedBook;
        private ViewModelBase? _prevViewModel;
        private ViewModelBase? _currentViewModel;

        public ViewModelBase? PrevViewModel
        {
            get => _prevViewModel;
            set =>  SetProperty(ref _prevViewModel, value);
        }
        public ViewModelBase? CurrentViewModel
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
                    OnPropertyChanged(nameof(IsGuest));
                    OnPropertyChanged(nameof(IsReader));
                    OnPropertyChanged(nameof(IsAdmin));
                    OnPropertyChanged(nameof(IsWriter));
                    OnPropertyChanged(nameof(IsAuthenticated));
                    OnPropertyChanged(nameof(CanManageBooks));
                    OnPropertyChanged(nameof(CanComment));
                }
            }
        }

        public bool IsGuest => CurrentUser == null;
        public bool IsAdmin => CurrentUser?.Role == UserRole.Admin;
        public bool IsWriter => CurrentUser?.Role == UserRole.Writer || IsAdmin;
        public bool IsReader => CurrentUser?.Role == UserRole.Reader || IsAdmin || IsWriter;
        public bool IsAuthenticated => !IsGuest;

        public bool CanManageBooks => IsAdmin || (IsWriter && _currentUser != null && _selectedBook != null && _currentUser.UserId == _selectedBook.PublisherId);
        public bool CanComment => !IsGuest && !(CurrentUser?.BlockedComments ?? false);

        public Book? SelectedBook
        {
            get => _selectedBook;
            set => SetProperty(ref _selectedBook, value);
        }
        public Language CurrentLanguage
        {
            get => _currentLanguage;
            set => SetProperty(ref _currentLanguage, value);
        }

        public string CurrentTheme
        {
            get => _currentTheme;
            set => SetProperty(ref _currentTheme, value);
        }
    }
}