using lc.Models;
using lc.Models.Enums;
using lc.ViewModels.Base;

namespace lc.Infrastructure;

public class AppState : ObservableObject
{
    private User?          _currentUser;
    private Book?          _selectedBook;
    private ViewModelBase? _prevViewModel;
    private ViewModelBase? _currentViewModel;

    private Language _currentLanguage = Language.Русский;
    private string _currentTheme = "Dark";

    public Language CurrentLanguage
    {
        get => _currentLanguage;
        set => SetProperty(ref _currentLanguage, value);
    }

    public string CurrentTheme
    {
        get => _currentTheme;
        set => SetProperty(ref _currentTheme, string.IsNullOrWhiteSpace(value) ? "Dark" : value.Trim());
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
                OnPropertyChanged(nameof(CanComment));
                OnPropertyChanged(nameof(CanManageBooks));
            }
        }
    }

    public Book? SelectedBook
    {
        get => _selectedBook;
        set
        {
            if (SetProperty(ref _selectedBook, value))
            {
                OnPropertyChanged(nameof(CanManageBooks));
            }
        }
    }

    public ViewModelBase? PrevViewModel
    {
        get => _prevViewModel;
        set => SetProperty(ref _prevViewModel, value);
    }

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set
        {
            var old = _currentViewModel;

            if (SetProperty(ref _currentViewModel, value))
            {
                _prevViewModel = old;
                OnPropertyChanged(nameof(PrevViewModel));
            }
        }
    }

    public bool IsGuest => CurrentUser is null;
    public bool IsAdmin => CurrentUser?.Role is UserRole.Admin;
    public bool IsWriter => CurrentUser?.Role is UserRole.Writer or UserRole.Admin;
    public bool IsReader => CurrentUser?.Role is UserRole.Reader or UserRole.Writer or UserRole.Admin;
    public bool IsAuthenticated => !IsGuest;
    public bool CanComment => !IsGuest && !(CurrentUser?.BlockedComments ?? false);
    public bool CanManageBooks => IsAdmin || (IsWriter && (SelectedBook is null || CurrentUser?.UserId == SelectedBook.PublisherId));
    public bool CanRequestAuthorRole => CurrentUser is not null && CurrentUser.Role == UserRole.Reader;

    public void RefreshCurrentUser()
    {
        OnPropertyChanged(nameof(CurrentUser));
    }
}