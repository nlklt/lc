using lc.Commands;
using lc.Data.Repositories.Interfaces;
using lc.Helpers;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace lc.ViewModels;

public sealed class ProfileViewModel : ViewModelBase, IDisposable
{
    private const int MinUserNameLength = 3;
    private const int MaxUserNameLength = 16;
    private const int MaxListNameLength = 16;
    private const int MaxAuthorRequestLength = 2000;

    private static readonly HashSet<string> ProtectedDefaultLists =
    [
        "Читаю",
        "В планах",
        "Брошено"
    ];

    private readonly AppState _appState;
    private readonly IUserRepository _userRepository;
    private readonly IAuthorRequestService _authorRequestService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly IDialogService _dialogService;
    private readonly IUserLibraryService _userLibraryService;
    private readonly IReadingHistoryRepository _readingHistoryRepository;
    private readonly IReadingProgressRepository _readingProgressRepository;
    private readonly IWindowService _windowService;

    private readonly SemaphoreSlim _dbLock = new(1, 1);

    private async Task RunDbOperationAsync(Func<Task> action)
    {
        await _dbLock.WaitAsync();
        try
        {
            await action();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    private int    _loadVersion;
    private bool   _isBusy;
    private bool   _isSettingsOpen;
    private bool   _isDisposed;
    private bool   _suppressSelectedListBooksLoad;
    private string _statusMessage = string.Empty;

    private string   _userName = string.Empty;
    private string?  _avatarPath;
    private bool     _blockedComments;
    private DateTime _createdAt;
    private UserRole _role = UserRole.Guest;
    private Language _preferredLanguage = Language.Русский;
    private string   _preferredTheme = "Dark";

    private string   _originalUserName = string.Empty;
    private string?  _originalAvatarPath;
    private Language _originalPreferredLanguage = Language.Русский;
    private string   _originalPreferredTheme = "Dark";

    private string _newListName = string.Empty;
    private string _selectedListName = string.Empty;
    private string _authorRequestMessage = string.Empty;

    private UserLibraryListDto? _selectedList;
    private AuthorRequest?      _latestAuthorRequest;
    private bool                _hasPendingAuthorRequest;

    private async Task SelectListAsync(object? parameter)
    {
        if (parameter is not UserLibraryListDto list)
            return;

        SelectedList = list;
        await LoadSelectedListBooksAsync(list.ListId);
    }

    public ProfileViewModel(
        AppState appState,
        IUserRepository userRepository,
        IAuthorRequestService authorRequestService,
        IAuthService authService,
        INavigationService navigationService,
        IThemeService themeService,
        ILocalizationService localizationService,
        IDialogService dialogService,
        IUserLibraryService userLibraryService,
        IReadingHistoryRepository readingHistoryRepository,
        IReadingProgressRepository readingProgressRepository,
        IWindowService windowService)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _authorRequestService = authorRequestService ?? throw new ArgumentNullException(nameof(authorRequestService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _userLibraryService = userLibraryService ?? throw new ArgumentNullException(nameof(userLibraryService));
        _readingHistoryRepository = readingHistoryRepository ?? throw new ArgumentNullException(nameof(readingHistoryRepository));
        _readingProgressRepository = readingProgressRepository ?? throw new ArgumentNullException(nameof(readingProgressRepository));
        _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));

        _appState.PropertyChanged += OnAppStatePropertyChanged;

        SelectListCommand       = new AsyncRelayCommand(SelectListAsync, _ => IsAuthenticated && !IsBusy);

        ToggleSettingsCommand   = new RelayCommand(_ => IsSettingsOpen = true, _ => !IsBusy);
        GoBackCommand           = new RelayCommand(_ => GoBack(), _ => true);

        SaveSettingsCommand     = new AsyncRelayCommand(_ => SaveSettingsAsync(), _ => CanSave);
        ResetSettingsCommand    = new RelayCommand(_ => LoadFromCurrentUser(), _ => !IsBusy);
        ChangeAvatarCommand     = new RelayCommand(_ => ChangeAvatar(), _ => !IsBusy && IsAuthenticated);
        ClearAvatarCommand      = new RelayCommand(_ => AvatarPath = null, _ => !IsBusy && !string.IsNullOrWhiteSpace(AvatarPath));
        DeleteAccountCommand    = new AsyncRelayCommand(_ => DeleteAccountAsync(), _ => IsAuthenticated && !IsBusy);
        LogoutCommand           = new AsyncRelayCommand(_ => LogoutAsync(), _ => IsAuthenticated && !IsBusy);

        LoadLibraryListsCommand           = new AsyncRelayCommand(_ => LoadLibraryListsAsync(), _ => IsAuthenticated && !IsBusy);
        CreateListCommand                 = new AsyncRelayCommand(_ => CreateListAsync(), _ => IsAuthenticated && !IsBusy && !string.IsNullOrWhiteSpace(NewListName));
        RenameSelectedListCommand         = new AsyncRelayCommand(_ => RenameSelectedListAsync(), _ => IsAuthenticated && !IsBusy && SelectedList is not null && SelectedListCanBeEdited && !string.IsNullOrWhiteSpace(SelectedListName));
        DeleteSelectedListCommand         = new AsyncRelayCommand(_ => DeleteSelectedListAsync(), _ => IsAuthenticated && !IsBusy && SelectedList is not null && SelectedListCanBeEdited);
        RemoveBookFromSelectedListCommand = new AsyncRelayCommand(RemoveBookFromSelectedListAsync, _ => IsAuthenticated && !IsBusy && SelectedList is not null && SelectedListCanBeEdited);

        OpenBookCommand = new RelayCommand(OpenBookDetails, _ => !IsBusy);

        OpenContinueReadingCommand  = new AsyncRelayCommand(OpenContinueReadingAsync, _ => !IsBusy);
        SendAuthorRequestCommand    = new AsyncRelayCommand(_ => SendAuthorRequestAsync(), _ => CanSendAuthorRequest);
        CancelAuthorRequestCommand  = new AsyncRelayCommand(_ => CancelAuthorRequestAsync(), _ => CanCancelAuthorRequest);
        LoadAuthorRequestsCommand   = new AsyncRelayCommand(_ => LoadAuthorRequestsAsync(), _ => IsAuthenticated && !IsBusy);

        LoadFromCurrentUser();
        _ = LoadInitialAsync();
    }

    // Identity / auth shortcuts

    public int CurrentUserId => _appState.CurrentUser?.UserId ?? 0;
    public User? CurrentUser =>         _appState.CurrentUser;
    public string CurrentUserName =>    _appState.CurrentUser?.UserName ?? "Гость";
    public UserRole CurrentUserRole =>  _appState.CurrentUser?.Role ?? UserRole.Guest;

    public bool IsReader => _appState.IsReader;
    public bool IsWriter => _appState.IsWriter;
    public bool IsAdmin => _appState.IsAdmin;
    public bool IsAuthenticated => _appState.IsAuthenticated;

    // Load-version helpers (cancel stale async results)

    private int NextLoadVersion() => Interlocked.Increment(ref _loadVersion);
    private bool IsCurrentLoad(int version) => version == Volatile.Read(ref _loadVersion);

    // Observable properties

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                RefreshCommandStates();
        }
    }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set => SetProperty(ref _isSettingsOpen, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string UserName
    {
        get => _userName;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (SetProperty(ref _userName, normalized))
                RefreshEditableState();
        }
    }

    public string? AvatarPath
    {
        get => _avatarPath;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (SetProperty(ref _avatarPath, normalized))
                RefreshEditableState();
        }
    }

    public bool BlockedComments
    {
        get => _blockedComments;
        private set => SetProperty(ref _blockedComments, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        private set => SetProperty(ref _createdAt, value);
    }

    public UserRole Role
    {
        get => _role;
        private set => SetProperty(ref _role, value);
    }

    public Language PreferredLanguage
    {
        get => _preferredLanguage;
        set
        {
            if (SetProperty(ref _preferredLanguage, value))
            {
                ApplyLanguagePreview(value);
                RefreshEditableState();
            }
        }
    }

    public string PreferredTheme
    {
        get => _preferredTheme;
        set
        {
            if (SetProperty(ref _preferredTheme, value))
            {
                ApplyThemePreview(value);
                RefreshEditableState();
            }
        }
    }

    public string NewListName
    {
        get => _newListName;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (SetProperty(ref _newListName, normalized))
                RefreshCommandStates();
        }
    }

    public string SelectedListName
    {
        get => _selectedListName;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (SetProperty(ref _selectedListName, normalized))
                RefreshCommandStates();
        }
    }

    public string AuthorRequestMessage
    {
        get => _authorRequestMessage;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (SetProperty(ref _authorRequestMessage, normalized))
                RefreshAuthorRequestState();
        }
    }

    public bool HasPendingAuthorRequest
    {
        get => _hasPendingAuthorRequest;
        private set
        {
            if (SetProperty(ref _hasPendingAuthorRequest, value))
                RefreshAuthorRequestState();
        }
    }

    public AuthorRequest? LatestAuthorRequest
    {
        get => _latestAuthorRequest;
        private set
        {
            if (SetProperty(ref _latestAuthorRequest, value))
                OnPropertyChanged(nameof(AuthorRequestStatusText));
        }
    }

    public string AuthorRequestStatusText =>
        LatestAuthorRequest is null
            ? "Заявок на повышение роли пока нет."
            : LatestAuthorRequest.Status switch
            {
                AuthorRequestStatus.Pending  => "Заявка на авторство уже отправлена и ожидает решения.",
                AuthorRequestStatus.Approved => "Последняя заявка одобрена.",
                AuthorRequestStatus.Rejected => "Последняя заявка отклонена.",
                _                            => "Статус заявки неизвестен."
            };

    public bool CanSave =>
        IsAuthenticated &&
        !IsBusy &&
        IsValidProfile() &&
        HasChanges();

    public bool CanSendAuthorRequest =>
        IsAuthenticated &&
        IsReader &&
        !IsBusy &&
        !HasPendingAuthorRequest &&
        !string.IsNullOrWhiteSpace(AuthorRequestMessage) &&
        AuthorRequestMessage.Length <= MaxAuthorRequestLength;

    public bool CanCancelAuthorRequest =>
        IsAuthenticated &&
        IsReader &&
        !IsBusy &&
        HasPendingAuthorRequest;

    public bool SelectedListCanBeEdited =>
        SelectedList is not null &&
        !IsProtectedList(SelectedList.Name);

    public IReadOnlyList<string> AvailableThemes { get; } = ["Light", "Dark"];
    public IEnumerable<Language> AllLanguages => [Language.Русский, Language.Английский];

    public ObservableCollection<ContinueReadingItemDto> ContinueReadingBooks { get; } = [];
    public ObservableCollection<UserLibraryListDto>     UserLibraryLists      { get; } = [];
    public ObservableCollection<BookListItemDto>        SelectedListBooks      { get; } = [];
    public ObservableCollection<AuthorRequest>          AuthorRequests         { get; } = [];

    public UserLibraryListDto? SelectedList
    {
        get => _selectedList;
        set
        {
            if (!SetProperty(ref _selectedList, value))
                return;

            SelectedListName = value?.Name ?? string.Empty;
            RefreshCommandStates();

            if (_suppressSelectedListBooksLoad)
                return;

            if (value is not null)
                _ = LoadSelectedListBooksAsync(value.ListId);
            else
                SelectedListBooks.Clear();
        }
    }

    // Commands

    public ICommand SelectListCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand ResetSettingsCommand { get; }
    public ICommand ChangeAvatarCommand { get; }
    public ICommand ClearAvatarCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand DeleteAccountCommand { get; }
    public ICommand ToggleSettingsCommand { get; }
    public ICommand GoBackCommand { get; }
    public ICommand LoadLibraryListsCommand { get; }
    public ICommand CreateListCommand { get; }
    public ICommand RenameSelectedListCommand { get; }
    public ICommand DeleteSelectedListCommand { get; }
    public ICommand RemoveBookFromSelectedListCommand { get; }
    public ICommand OpenBookCommand { get; }
    public ICommand OpenContinueReadingCommand { get; }
    public ICommand SendAuthorRequestCommand { get; }
    public ICommand CancelAuthorRequestCommand { get; }
    public ICommand LoadAuthorRequestsCommand { get; }

    // Initial load / app-state reaction

    private async Task LoadInitialAsync()
    {
        var version = NextLoadVersion();

        if (!IsAuthenticated)
        {
            ClearLoadedData();
            return;
        }

        await LoadLibraryListsAsync(version);
        if (!IsCurrentLoad(version)) return;

        await LoadContinueReadingAsync(version);
        if (!IsCurrentLoad(version)) return;

        await LoadAuthorRequestsAsync(version);
    }

    private void OnAppStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AppState.CurrentUser))
            return;

        LoadFromCurrentUser();
        _ = LoadInitialAsync();
    }

    // User settings

    private void LoadFromCurrentUser()
    {
        var user = _appState.CurrentUser;

        if (user is null)
        {
            UserName = string.Empty;
            AvatarPath = null;
            BlockedComments = false;
            CreatedAt = default;
            Role = UserRole.Guest;
            PreferredLanguage = _appState.CurrentLanguage;
            PreferredTheme = _appState.CurrentTheme;

            StoreOriginalValues();
            StatusMessage = string.Empty;
            LatestAuthorRequest = null;
            HasPendingAuthorRequest = false;
            RefreshCommandStates();
            return;
        }

        UserName = user.UserName;
        AvatarPath = user.AvatarPath;
        BlockedComments = user.BlockedComments;
        CreatedAt = user.CreatedAt;
        Role = user.Role;
        PreferredLanguage = user.PreferredLanguage;
        PreferredTheme = string.IsNullOrWhiteSpace(user.PreferredTheme) ? "Dark" : user.PreferredTheme;

        StoreOriginalValues();
        StatusMessage = string.Empty;
        RefreshCommandStates();
    }

    private void StoreOriginalValues()
    {
        _originalUserName = UserName;
        _originalAvatarPath = AvatarPath;
        _originalPreferredLanguage = PreferredLanguage;
        _originalPreferredTheme = PreferredTheme;
    }

    private bool HasChanges() =>
        !string.Equals(UserName.Trim(), _originalUserName.Trim(), StringComparison.Ordinal) ||
        !string.Equals(Normalize(AvatarPath), Normalize(_originalAvatarPath), StringComparison.Ordinal) ||
        PreferredLanguage != _originalPreferredLanguage ||
        !string.Equals(PreferredTheme.Trim(), _originalPreferredTheme.Trim(), StringComparison.Ordinal);

    private bool IsValidProfile()
    {
        if (string.IsNullOrWhiteSpace(UserName))
            return false;

        if (UserName.Length < MinUserNameLength || UserName.Length > MaxUserNameLength)
            return false;

        if (!string.IsNullOrWhiteSpace(AvatarPath) && !File.Exists(AvatarPath))
            return false;

        if (!AvailableThemes.Contains(PreferredTheme))
            return false;

        return PreferredLanguage is Language.Русский or Language.Английский;
    }

    private async Task SaveSettingsAsync()
    {
        if (!IsAuthenticated || _appState.CurrentUser is null)
        {
            StatusMessage = "Пользователь не авторизован.";
            return;
        }

        if (!CanSave)
        {
            StatusMessage = "Проверьте корректность данных профиля.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            var user = _appState.CurrentUser;
            var normalizedUserName = UserName.Trim();
            var normalizedAvatar   = Normalize(AvatarPath);
            var normalizedTheme    = string.IsNullOrWhiteSpace(PreferredTheme) ? "Dark" : PreferredTheme.Trim();

            if (!string.Equals(user.UserName, normalizedUserName, StringComparison.Ordinal))
            {
                if (await _userRepository.ExistsByUserNameAsync(normalizedUserName))
                {
                    StatusMessage = "Пользователь с таким именем уже существует.";
                    return;
                }
            }

            user.UserName = normalizedUserName;
            user.AvatarPath = normalizedAvatar;
            user.PreferredLanguage = PreferredLanguage;
            user.PreferredTheme = normalizedTheme;
            user.BlockedComments = BlockedComments;

            var updated = await _userRepository.UpdateAsync(user);
            if (!updated)
            {
                StatusMessage = "Не удалось сохранить профиль.";
                return;
            }

            _appState.CurrentUser = user;
            _appState.CurrentLanguage = user.PreferredLanguage;
            _appState.CurrentTheme = user.PreferredTheme;

            StoreOriginalValues();
            NotifyCurrentUserChanged();
            StatusMessage = "Настройки сохранены.";
        }
        catch
        {
            StatusMessage = "Ошибка сохранения профиля.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NotifyCurrentUserChanged()
    {
        OnPropertyChanged(nameof(CurrentUser));
        OnPropertyChanged(nameof(CurrentUserName));
        OnPropertyChanged(nameof(CurrentUserRole));
    }

    private void ChangeAvatar()
    {
        var path = _dialogService.OpenFile(
            "Выберите изображение",
            "Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp");

        if (!string.IsNullOrWhiteSpace(path))
            AvatarPath = path;
    }

    private async Task LogoutAsync()
    {
        if (!IsAuthenticated)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync("Выход", "Выйти из аккаунта?");
        if (!confirmed)
            return;

        _authService.Logout();
        _navigationService.NavigateTo<CatalogViewModel>();
    }

    private async Task DeleteAccountAsync()
    {
        if (!IsAuthenticated || _appState.CurrentUser is null)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Удаление аккаунта",
            "Аккаунт будет удалён без возможности восстановления. Продолжить?");

        if (!confirmed)
            return;

        try
        {
            IsBusy = true;

            var userId = _appState.CurrentUser.UserId;
            await _userRepository.DeleteAsync(userId);

            _authService.Logout();
            _navigationService.NavigateTo<CatalogViewModel>();
        }
        catch
        {
            StatusMessage = "Не удалось удалить аккаунт.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void GoBack()
    {
        if (IsSettingsOpen)
        {
            IsSettingsOpen = false;
            return;
        }

        _navigationService.NavigateBack();
    }

    // Library lists

    private Task LoadLibraryListsAsync() => LoadLibraryListsAsync(NextLoadVersion());

    private async Task LoadLibraryListsAsync(int version)
    {
        if (!IsAuthenticated || _appState.CurrentUser is null)
        {
            UserLibraryLists.Clear();
            SelectedListBooks.Clear();
            SelectedList = null;
            return;
        }

        try
        {
            IsBusy = true;

            await _userLibraryService.EnsureDefaultListsAsync();

            var lists = (await _userLibraryService.GetListsAsync()).ToList();
            if (!IsCurrentLoad(version))
                return;

            ReplaceCollection(UserLibraryLists, lists);

            var nextSelected = SelectedList is null
                ? UserLibraryLists.FirstOrDefault()
                : UserLibraryLists.FirstOrDefault(x => x.ListId == SelectedList.ListId)
                  ?? UserLibraryLists.FirstOrDefault();

            _suppressSelectedListBooksLoad = true;
            try
            {
                SelectedList = nextSelected;
            }
            finally
            {
                _suppressSelectedListBooksLoad = false;
            }

            if (SelectedList is not null)
                await LoadSelectedListBooksAsync(SelectedList.ListId);
            else
                SelectedListBooks.Clear();
        }
        catch
        {
            StatusMessage = "Не удалось загрузить списки.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadSelectedListBooksAsync(int? selectedListId)
    {
        if (!IsAuthenticated || selectedListId is null)
        {
            SelectedListBooks.Clear();
            return;
        }

        try
        {
            IsBusy = true;

            var books = await _userLibraryService.GetBooksFromListAsync(selectedListId.Value);

            // Guard: the user may have switched lists while we were awaiting.
            if (SelectedList?.ListId != selectedListId.Value)
                return;
            if (books != null && books.Count > 0)
            {
                ReplaceCollection(SelectedListBooks, books);
            }
        }
        catch
        {
            StatusMessage = "Не удалось загрузить книги списка.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateListAsync()
    {
        if (!IsAuthenticated)
            return;

        var name = NewListName.Trim();
        if (!ValidateListName(name, out var error))
        {
            StatusMessage = error;
            return;
        }

        try
        {
            IsBusy = true;

            await _userLibraryService.CreateListAsync(name);
            NewListName = string.Empty;
            await LoadLibraryListsAsync();
        }
        catch
        {
            StatusMessage = "Не удалось создать список.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RenameSelectedListAsync()
    {
        if (SelectedList is null || !SelectedListCanBeEdited)
            return;

        var newName = SelectedListName.Trim();
        if (!ValidateListName(newName, out var error))
        {
            StatusMessage = error;
            return;
        }

        if (UserLibraryLists.Any(x =>
                x.ListId != SelectedList.ListId &&
                string.Equals(x.Name, newName, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = "Список с таким названием уже существует.";
            return;
        }

        try
        {
            IsBusy = true;

            await _userLibraryService.RenameListAsync(SelectedList.ListId, newName);
            await LoadLibraryListsAsync();
        }
        catch
        {
            StatusMessage = "Не удалось переименовать список.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteSelectedListAsync()
    {
        if (SelectedList is null || !SelectedListCanBeEdited)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Удалить список",
            $"Удалить список «{SelectedList.Name}»?");

        if (!confirmed)
            return;

        try
        {
            IsBusy = true;

            await _userLibraryService.DeleteListAsync(SelectedList.ListId);
            await LoadLibraryListsAsync();
        }
        catch
        {
            StatusMessage = "Не удалось удалить список.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Books

    private void OpenBookDetails(object? parameter)
    {
        if (parameter is not BookListItemDto book)
            return;

        _navigationService.NavigateTo<BookDetailsViewModel>(book.BookId);
    }

    private async Task RemoveBookFromSelectedListAsync(object? parameter)
    {
        if (SelectedList is null || parameter is not BookListItemDto book)
            return;

        try
        {
            IsBusy = true;

            await _userLibraryService.RemoveBookFromListAsync(SelectedList.ListId, book.BookId);
            await LoadSelectedListBooksAsync(SelectedList.ListId);
        }
        catch
        {
            StatusMessage = "Не удалось удалить книгу из списка.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Continue reading

    private Task LoadContinueReadingAsync() => LoadContinueReadingAsync(NextLoadVersion());

    private async Task LoadContinueReadingAsync(int version)
    {
        if (!IsAuthenticated || _appState.CurrentUser is null)
        {
            ContinueReadingBooks.Clear();
            return;
        }

        await RunDbOperationAsync(async () =>
        {
            try
            {
                IsBusy = true;

                var userId = _appState.CurrentUser.UserId;

                var histories = (await _readingHistoryRepository.GetByUserIdAsync(userId))
                    .Where(x => x.Book is not null && x.Book.BookStatus == BookStatus.Published)
                    .GroupBy(x => x.BookId)
                    .Select(g => g.OrderByDescending(x => x.LastOpenedAt).First())
                    .OrderByDescending(x => x.LastOpenedAt)
                    .Take(10)
                    .ToList();

                if (!IsCurrentLoad(version)) return;

                var progressEntries = await _readingProgressRepository.GetByUserIdAsync(userId);

                if (!IsCurrentLoad(version)) return;

                var progressByBook = progressEntries
                    .Where(x => x.Chapter?.BookId > 0)
                    .GroupBy(x => x.Chapter!.BookId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(x => x.UpdatedAt).First());

                var items = new List<ContinueReadingItemDto>();

                foreach (var history in histories)
                {
                    progressByBook.TryGetValue(history.BookId, out var progress);

                    items.Add(new ContinueReadingItemDto
                    {
                        BookId = history.BookId,
                        Title = history.Book?.Title ?? "Без названия",
                        CoverPath = history.Book?.CoverImagePath,
                        LastChapterNumber = progress?.Chapter?.ChapterNumber,
                        ReadingProgressPercent = progress?.ProgressPercent ?? 0,
                        LastOpenedAt = history.LastOpenedAt
                    });
                }

                ReplaceCollection(ContinueReadingBooks, items);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message; // !!!
            }
            finally
            {
                IsBusy = false;
            }
        });
    }

    private async Task OpenContinueReadingAsync(object? parameter)
    {
        if (parameter is not ContinueReadingItemDto item || item.BookId <= 0)
            return;

        await _windowService.OpenReaderAsync(item.BookId, item.LastChapterNumber);
    }

    // Author requests

    private Task LoadAuthorRequestsAsync() => LoadAuthorRequestsAsync(NextLoadVersion());

    private async Task LoadAuthorRequestsAsync(int version)
    {
        if (!IsAuthenticated || _appState.CurrentUser is null)
        {
            AuthorRequests.Clear();
            LatestAuthorRequest = null;
            HasPendingAuthorRequest = false;
            return;
        }

        try
        {
            IsBusy = true;

            var requests = (await _authorRequestService.GetMyRequestsAsync(_appState.CurrentUser.UserId))
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            if (!IsCurrentLoad(version)) return;

            ReplaceCollection(AuthorRequests, requests);

            LatestAuthorRequest = requests.FirstOrDefault();
            HasPendingAuthorRequest = requests.Any(x => x.Status == AuthorRequestStatus.Pending);
        }
        catch (Exception ex)
        {
            // временно:
            StatusMessage = $"Ошибка: {ex.Message} | {ex.InnerException?.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SendAuthorRequestAsync()
    {
        if (!IsAuthenticated || _appState.CurrentUser is null)
            return;

        if (!CanSendAuthorRequest)
        {
            StatusMessage = "Проверьте текст заявки.";
            return;
        }

        try
        {
            IsBusy = true;

            await _authorRequestService.SendRequestAsync(
                _appState.CurrentUser.UserId,
                AuthorRequestMessage.Trim());

            AuthorRequestMessage = string.Empty;
            await LoadAuthorRequestsAsync();
            StatusMessage = "Заявка успешно отправлена.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CancelAuthorRequestAsync()
    {
        if (!IsAuthenticated || _appState.CurrentUser is null || !HasPendingAuthorRequest)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Отменить заявку",
            "Отменить текущую заявку на авторство?");

        if (!confirmed)
            return;

        try
        {
            IsBusy = true;

            await _authorRequestService.CancelPendingRequestAsync(_appState.CurrentUser.UserId);
            await LoadAuthorRequestsAsync();
            StatusMessage = "Заявка отменена.";
        }
        catch
        {
            StatusMessage = "Не удалось отменить заявку.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Validation helpers

    private bool ValidateListName(string name, out string error)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Введите название списка.";
            return false;
        }

        if (name.Length > MaxListNameLength)
        {
            error = "Название списка слишком длинное.";
            return false;
        }

        if (UserLibraryLists.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            error = "Список с таким названием уже существует.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool IsProtectedList(string name)
        => ProtectedDefaultLists.Contains(name);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // Theme / language preview

    private void ApplyThemePreview(string themeName)
    {
        if (!string.Equals(_appState.CurrentTheme, themeName, StringComparison.Ordinal))
        {
            _themeService.SetTheme(themeName);
            _appState.CurrentTheme = themeName;
        }
    }

    private void ApplyLanguagePreview(Language language)
    {
        var code = language switch
        {
            Language.Русский    => "ru",
            Language.Английский => "en",
            _                   => "ru"
        };

        if (_appState.CurrentLanguage != language)
        {
            _localizationService.SetLanguage(code);
            _appState.CurrentLanguage = language;
        }
    }

    // Collection / UI helpers

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
            target.Add(item);
    }

    private void ClearLoadedData()
    {
        ContinueReadingBooks.Clear();
        UserLibraryLists.Clear();
        SelectedListBooks.Clear();
        AuthorRequests.Clear();
        LatestAuthorRequest = null;
        HasPendingAuthorRequest = false;
    }

    private void RefreshEditableState()
    {
        OnPropertyChanged(nameof(CanSave));
        RefreshCommandStates();
    }

    private void RefreshAuthorRequestState()
    {
        OnPropertyChanged(nameof(AuthorRequestStatusText));
        OnPropertyChanged(nameof(CanSendAuthorRequest));
        OnPropertyChanged(nameof(CanCancelAuthorRequest));
        RefreshCommandStates();
    }

    private void RefreshCommandStates()
    {
        (SaveSettingsCommand               as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ResetSettingsCommand              as RelayCommand)?.RaiseCanExecuteChanged();
        (ChangeAvatarCommand               as RelayCommand)?.RaiseCanExecuteChanged();
        (ClearAvatarCommand                as RelayCommand)?.RaiseCanExecuteChanged();
        (LogoutCommand                     as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (DeleteAccountCommand              as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ToggleSettingsCommand             as RelayCommand)?.RaiseCanExecuteChanged();
        (GoBackCommand                     as RelayCommand)?.RaiseCanExecuteChanged();

        (LoadLibraryListsCommand           as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CreateListCommand                 as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (RenameSelectedListCommand         as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (DeleteSelectedListCommand         as AsyncRelayCommand)?.RaiseCanExecuteChanged(); 
        (SelectListCommand                 as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (RemoveBookFromSelectedListCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (OpenBookCommand                   as RelayCommand)?.RaiseCanExecuteChanged();

        (OpenContinueReadingCommand        as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (SendAuthorRequestCommand          as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CancelAuthorRequestCommand        as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (LoadAuthorRequestsCommand         as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    // IDisposable

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _appState.PropertyChanged -= OnAppStatePropertyChanged;
    }
}