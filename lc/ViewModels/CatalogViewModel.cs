using lc.Commands;
using lc.Helpers;
using lc.Infrastructure;
using lc.Models;
using lc.Models.Enums;
using lc.Services;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows.Input;

namespace lc.ViewModels;

public sealed class CatalogViewModel : ViewModelBase, IDisposable
{
    private readonly AppState            _appState;
    private readonly IBookService        _bookService;
    private readonly IDialogService      _dialogService;
    private readonly INavigationService  _navigationService;
    private readonly IUserLibraryService _userLibraryService;

    private readonly SemaphoreSlim   _refreshGate = new(1, 1);
    private CancellationTokenSource? _refreshDebounceCts;

    private bool _isDisposed;
    private bool _isInitialized;
    private bool _suppressAutoRefresh;
    private int  _refreshVersion;

    private string  _searchText = string.Empty;
    private string  _selectedSortField = nameof(BookListItemDto.Title);
    private bool    _sortAscending = true;
    private bool    _isLoading;
    private bool    _isCategoriesOpen;
    private bool    _isTagsOpen;
    private bool    _isStrictCategoryMatch;
    private bool    _isStrictTagMatch;
    private string  _errorMessage = string.Empty;

    private ObservableCollection<BookListItemDto>               _books = [];
    private ObservableCollection<TriStateOption<Category>>      _categoryOptions = [];
    private ObservableCollection<TriStateOption<Tag>>           _tagOptions = [];
    private ObservableCollection<TriStateOption<WritingStatus>> _writingStatusOptions = [];
    private ObservableCollection<TriStateOption<BookStatus>>    _bookStatusOptions = [];
    private ObservableCollection<TriStateOption<Language>>      _languageOptions = [];
    private ObservableCollection<TriStateOption<int>>           _ageRatingOptions = [];

    private BookListItemDto? _selectedBook;

    public CatalogViewModel(
        AppState            appState,
        IBookService        bookService,
        IDialogService      dialogService,
        INavigationService  navigationService,
        IUserLibraryService userLibraryService)
    {
        _appState           = appState ?? throw new ArgumentNullException(nameof(appState));
        _bookService        = bookService ?? throw new ArgumentNullException(nameof(bookService));
        _dialogService      = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _navigationService  = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _userLibraryService = userLibraryService ?? throw new ArgumentNullException(nameof(userLibraryService));

        InitializeCommand       = new AsyncRelayCommand(_ => InitializeAsync());
        ToggleCategoriesCommand = new RelayCommand(_ => ToggleCategories());
        ToggleTagsCommand       = new RelayCommand(_ => ToggleTags());

        ApplyFiltersCommand = new AsyncRelayCommand(_ => RequestRefreshAsync());
        ResetFiltersCommand = new AsyncRelayCommand(_ => ResetFiltersAsync());

        OpenDetailsCommand  = new RelayCommand(OpenDetails);
        StartReadingCommand = new RelayCommand(StartReading);

        AddToListCommand = new AsyncRelayCommand(AddToLibraryAsync);

        AddBookCommand      = new RelayCommand(_ => AddBook(), _ => CanManageBooks);
        EditBookCommand     = new RelayCommand(EditBook, _ => CanManageBooks);
        ArchiveBookCommand  = new AsyncRelayCommand(_ => ArchiveBookAsync(), _ => CanManageBooks);
        DeleteBookCommand   = new AsyncRelayCommand(_ => DeleteBookAsync(), _ => IsAdmin);

        _appState.PropertyChanged += AppStateOnPropertyChanged;

        BuildStaticFilterOptions();
        _ = InitializeAsync();
    }

    public ICommand InitializeCommand { get; }
    public ICommand ToggleCategoriesCommand { get; }
    public ICommand ToggleTagsCommand { get; }
    public ICommand ApplyFiltersCommand { get; }
    public ICommand ResetFiltersCommand { get; }

    public ICommand OpenDetailsCommand { get; }
    public ICommand StartReadingCommand { get; }
    public ICommand AddToListCommand { get; }

    public ICommand AddBookCommand { get; }
    public ICommand EditBookCommand { get; }
    public ICommand ArchiveBookCommand { get; }
    public ICommand DeleteBookCommand { get; }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ObservableCollection<BookListItemDto> Books
    {
        get => _books;
        private set => SetProperty(ref _books, value);
    }

    public BookListItemDto? SelectedBook
    {
        get => _selectedBook;
        set
        {
            if (SetProperty(ref _selectedBook, value)) RaiseCommandState();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;

            if (SetProperty(ref _searchText, normalized)) OnFilterChanged();
        }
    }

    public string SelectedSortField
    {
        get => _selectedSortField;
        set
        {
            var normalized = SortFields.Contains(value) ? value : nameof(BookListItemDto.Title);

            if (SetProperty(ref _selectedSortField, normalized)) OnFilterChanged();
        }
    }

    public bool SortAscending
    {
        get => _sortAscending;
        set
        {
            if (SetProperty(ref _sortAscending, value)) OnFilterChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value)) RaiseCommandState();
        }
    }

    public bool IsCategoriesOpen
    {
        get => _isCategoriesOpen;
        set
        {
            if (SetProperty(ref _isCategoriesOpen, value))
            {
                if (value && _isTagsOpen)
                {
                    _isTagsOpen = false;
                    OnPropertyChanged(nameof(IsTagsOpen));
                }
            }
        }
    }

    public bool IsTagsOpen
    {
        get => _isTagsOpen;
        set
        {
            if (SetProperty(ref _isTagsOpen, value))
            {
                if (value && _isCategoriesOpen)
                {
                    _isCategoriesOpen = false;
                    OnPropertyChanged(nameof(IsCategoriesOpen));
                }
            }
        }
    }

    public bool IsStrictCategoryMatch
    {
        get => _isStrictCategoryMatch;
        set
        {
            if (SetProperty(ref _isStrictCategoryMatch, value)) OnFilterChanged();
        }
    }

    public bool IsStrictTagMatch
    {
        get => _isStrictTagMatch;
        set
        {
            if (SetProperty(ref _isStrictTagMatch, value)) OnFilterChanged();
        }
    }

    public bool IsAdmin =>          _appState.IsAdmin;
    public bool IsAuthenticated =>  _appState.IsAuthenticated;
    public bool CanManageBooks =>   _appState.CanManageBooks;
    public bool CanEditSelectedBook =>   CanManageBooks && SelectedBook is not null;
    public bool CanDeleteSelectedBook => IsAdmin && SelectedBook is not null;

    public Range<decimal>  Rating { get; } = new();
    public Range<int>      Chapters { get; } = new();
    public Range<int>      Symbols { get; } = new();
    public Range<DateTime> CreatedAt { get; } = new();

    public ObservableCollection<TriStateOption<Category>> CategoryOptions
    {
        get => _categoryOptions;
        private set => SetProperty(ref _categoryOptions, value);
    }

    public ObservableCollection<TriStateOption<Tag>> TagOptions
    {
        get => _tagOptions;
        private set => SetProperty(ref _tagOptions, value);
    }

    public ObservableCollection<TriStateOption<WritingStatus>> WritingStatusOptions
    {
        get => _writingStatusOptions;
        private set => SetProperty(ref _writingStatusOptions, value);
    }

    public ObservableCollection<TriStateOption<BookStatus>> BookStatusOptions
    {
        get => _bookStatusOptions;
        private set => SetProperty(ref _bookStatusOptions, value);
    }

    public ObservableCollection<TriStateOption<Language>> LanguageOptions
    {
        get => _languageOptions;
        private set => SetProperty(ref _languageOptions, value);
    }

    public ObservableCollection<TriStateOption<int>> AgeRatingOptions
    {
        get => _ageRatingOptions;
        private set => SetProperty(ref _ageRatingOptions, value);
    }

    public ObservableCollection<string> SortFields { get; } =
    [
        nameof(BookListItemDto.Title),
        nameof(BookListItemDto.AuthorName),
        nameof(BookListItemDto.CreatedAt),
        nameof(BookListItemDto.Rating),
        nameof(BookListItemDto.Views),
        nameof(BookListItemDto.ChaptersCount),
        nameof(BookListItemDto.SymbolsCount),
        nameof(BookListItemDto.BookStatus),
        nameof(BookListItemDto.WritingStatus)
    ];

    public string CategoriesSummaryText => GetSummary(CategoryOptions);
    public string TagsSummaryText => GetSummary(TagOptions);

    public async Task InitializeAsync()
    {
        if (_isDisposed || _isInitialized) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            HookRangeRefreshes();
            BuildStaticFilterOptions();
            await LoadLookupOptionsAsync();

            _isInitialized = true;
            await RequestRefreshAsync();
        }
        catch (Exception) { ErrorMessage = "Не удалось инициализировать каталог."; }
        finally { IsLoading = false; }
    }

    private void HookRangeRefreshes()
    {
        Rating.PropertyChanged    += RangeOnPropertyChanged;
        Chapters.PropertyChanged  += RangeOnPropertyChanged;
        Symbols.PropertyChanged   += RangeOnPropertyChanged;
        CreatedAt.PropertyChanged += RangeOnPropertyChanged;
    }

    private void RangeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Range<int>.From) or nameof(Range<int>.To) ||
            e.PropertyName is nameof(Range<decimal>.From) or nameof(Range<decimal>.To) ||
            e.PropertyName is nameof(Range<DateTime>.From) or nameof(Range<DateTime>.To))
        {
            OnFilterChanged();
        }
    }

    private async Task LoadLookupOptionsAsync()
    {
        var categories = await _bookService.GetAllCategoriesAsync();
        var tags = await _bookService.GetAllTagsAsync();

        CategoryOptions = new ObservableCollection<TriStateOption<Category>>(
            categories.Select(x => new TriStateOption<Category>(x, x.Name)));

        TagOptions = new ObservableCollection<TriStateOption<Tag>>(
            tags.Select(x => new TriStateOption<Tag>(x, x.Name)));

        HookOptionRefresh(CategoryOptions, nameof(CategoriesSummaryText));
        HookOptionRefresh(TagOptions, nameof(TagsSummaryText));
    }

    private void BuildStaticFilterOptions()
    {
        WritingStatusOptions = new ObservableCollection<TriStateOption<WritingStatus>>(
            Enum.GetValues<WritingStatus>().Select(x => new TriStateOption<WritingStatus>(x, x.ToString())));

        BookStatusOptions = new ObservableCollection<TriStateOption<BookStatus>>(
            Enum.GetValues<BookStatus>().Select(x => new TriStateOption<BookStatus>(x, x.ToString())));

        LanguageOptions = new ObservableCollection<TriStateOption<Language>>(
            Enum.GetValues<Language>().Select(x => new TriStateOption<Language>(x, x.ToString())));

        AgeRatingOptions = new ObservableCollection<TriStateOption<int>>
        {
            new(3, "3+"),
            new(6, "6+"),
            new(12, "12+"),
            new(16, "16+"),
            new(18, "18+")
        };

        HookOptionRefresh(WritingStatusOptions, null);
        HookOptionRefresh(BookStatusOptions, null);
        HookOptionRefresh(LanguageOptions, null);
        HookOptionRefresh(AgeRatingOptions, null);
    }

    private void HookOptionRefresh<T>(
        ObservableCollection<TriStateOption<T>> options,
        string? summaryPropertyName)
    {
        foreach (var option in options)
        {
            option.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(TriStateOption<T>.State))
                    return;

                if (!string.IsNullOrWhiteSpace(summaryPropertyName))
                    OnPropertyChanged(summaryPropertyName);

                OnFilterChanged();
            };
        }
    }

    private void OnFilterChanged()
    {
        if (!_isInitialized || _isDisposed || _suppressAutoRefresh)
            return;

        _ = DebouncedRefreshAsync();
    }

    private async Task DebouncedRefreshAsync()
    {
        try
        {
            _refreshDebounceCts?.Cancel();
            _refreshDebounceCts?.Dispose();

            var cts = new CancellationTokenSource();
            _refreshDebounceCts = cts;

            await Task.Delay(250, cts.Token);

            await RequestRefreshAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RequestRefreshAsync()
    {
        if (!_isInitialized || _isDisposed)
            return;

        if (!await _refreshGate.WaitAsync(0))
            return;

        IsLoading = true;

        try
        {
            while (true)
            {
                var versionAtStart = Volatile.Read(ref _refreshVersion);

                await ApplyFiltersCoreAsync();

                if (versionAtStart == Volatile.Read(ref _refreshVersion))
                    break;
            }
        }
        catch (Exception) { ErrorMessage = "Не удалось загрузить каталог."; }
        finally
        {
            IsLoading = false;
            _refreshGate.Release();
        }
    }

    private async Task ApplyFiltersCoreAsync()
    {
        ErrorMessage = string.Empty;

        var criteria = BuildCriteria();
        var items = await _bookService.GetCatalogAsync(criteria);

        Books = new ObservableCollection<BookListItemDto>(items);
    }

    private BookFilterCriteria BuildCriteria()
    {
        return new BookFilterCriteria
        {
            SearchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,

            IncludeTagIds = [.. TagOptions.Where(x => x.State == CheckBoxState.Include).Select(x => x.Value.TagId)],
            ExcludeTagIds = [.. TagOptions.Where(x => x.State == CheckBoxState.Exclude).Select(x => x.Value.TagId)],

            IncludeCategoryIds = [.. CategoryOptions.Where(x => x.State == CheckBoxState.Include).Select(x => x.Value.CategoryId)],
            ExcludeCategoryIds = [.. CategoryOptions.Where(x => x.State == CheckBoxState.Exclude).Select(x => x.Value.CategoryId)],

            StrictTagMatch = IsStrictTagMatch,
            StrictCategoryMatch = IsStrictCategoryMatch,

            IncludeWritingStatuses = [.. WritingStatusOptions.Where(x => x.State == CheckBoxState.Include).Select(x => x.Value)],
            ExcludeWritingStatuses = [.. WritingStatusOptions.Where(x => x.State == CheckBoxState.Exclude).Select(x => x.Value)],

            IncludeBookStatuses = [.. BookStatusOptions.Where(x => x.State == CheckBoxState.Include).Select(x => x.Value)],
            ExcludeBookStatuses = [.. BookStatusOptions.Where(x => x.State == CheckBoxState.Exclude).Select(x => x.Value)],

            IncludeLanguages = [.. LanguageOptions.Where(x => x.State == CheckBoxState.Include).Select(x => x.Value)],
            ExcludeLanguages = [.. LanguageOptions.Where(x => x.State == CheckBoxState.Exclude).Select(x => x.Value)],

            IncludeAgeRatings = [.. AgeRatingOptions.Where(x => x.State == CheckBoxState.Include).Select(x => x.Value)],
            ExcludeAgeRatings = [.. AgeRatingOptions.Where(x => x.State == CheckBoxState.Exclude).Select(x => x.Value)],

            RatingFrom = Rating.From,
            RatingTo = Rating.To,
            ChaptersFrom = Chapters.From,
            ChaptersTo = Chapters.To,
            SymbolsFrom = Symbols.From,
            SymbolsTo = Symbols.To,
            CreatedFrom = CreatedAt.From,
            CreatedTo = CreatedAt.To,

            SortField = SortFields.Contains(SelectedSortField) ? SelectedSortField : nameof(BookListItemDto.Title),
            SortAscending = SortAscending
        };
    }

    private async Task ResetFiltersAsync()
    {
        _suppressAutoRefresh = true;
        try
        {
            SearchText = string.Empty;
            SelectedSortField = nameof(BookListItemDto.Title);
            SortAscending = true;

            Rating.From = null;
            Rating.To = null;

            Chapters.From = null;
            Chapters.To = null;

            Symbols.From = null;
            Symbols.To = null;

            CreatedAt.From = null;
            CreatedAt.To = null;

            ClearTriState(CategoryOptions);
            ClearTriState(TagOptions);
            ClearTriState(WritingStatusOptions);
            ClearTriState(BookStatusOptions);
            ClearTriState(LanguageOptions);
            ClearTriState(AgeRatingOptions);
        }
        finally
        {
            _suppressAutoRefresh = false;
        }

        await RequestRefreshAsync();
    }

    private void ClearTriState<T>(ObservableCollection<TriStateOption<T>> options)
    {
        foreach (var opt in options)
            opt.State = CheckBoxState.Neutral;
    }

    private void AddBook()
    {
        _navigationService.NavigateTo<EditBookViewModel>();
    }

    private void EditBook(object? parameter)
    {
        var book = parameter as BookListItemDto ?? SelectedBook;
        if (book is null)
            return;

        _navigationService.NavigateTo<EditBookViewModel>(book.BookId);
    }

    private async Task ArchiveBookAsync()
    {
        var book = SelectedBook;
        if (book is null || !CanManageBooks)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Архивировать книгу",
            $"Поместить книгу «{book.Title}» в архив?");

        if (!confirmed)
            return;

        await _bookService.ArchiveBookAsync(book.BookId);
        await RequestRefreshAsync();
    }

    private async Task DeleteBookAsync()
    {
        var book = SelectedBook;
        if (book is null || !IsAdmin)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Удалить книгу",
            $"Удалить книгу «{book.Title}» полностью?");

        if (!confirmed)
            return;

        await _bookService.DeleteBookAsync(book.BookId);

        if (SelectedBook?.BookId == book.BookId)
            SelectedBook = null;

        await RequestRefreshAsync();
    }

    private async Task AddToLibraryAsync(object? parameter)
    {
        var book = parameter as BookListItemDto ?? SelectedBook;
        if (book is null)
            return;

        if (!_appState.IsAuthenticated)
        {
            await _dialogService.ShowMessageAsync(
                "Требуется вход",
                "Чтобы добавить книгу в библиотеку, нужно войти в аккаунт.");
            return;
        }

        await _userLibraryService.AddToLibraryAsync(book.BookId);
    }

    private void OpenDetails(object? parameter)
    {
        var book = parameter as BookListItemDto ?? SelectedBook;
        if (book is null)
            return;

        _navigationService.NavigateTo<BookDetailsViewModel>(book.BookId);
    }

    private void StartReading(object? parameter)
    {
        var book = parameter as BookListItemDto ?? SelectedBook;
        if (book is null)
            return;

        _navigationService.NavigateTo<ReaderViewModel>(book.BookId);
    }

    private void ToggleCategories()
    {
        IsCategoriesOpen = !IsCategoriesOpen;
    }

    private void ToggleTags()
    {
        IsTagsOpen = !IsTagsOpen;
    }

    private void AppStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(AppState.CurrentUser) and not nameof(AppState.SelectedBook))
            return;

        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(CanManageBooks));
        OnPropertyChanged(nameof(CanEditSelectedBook));
        OnPropertyChanged(nameof(CanDeleteSelectedBook));
        RaiseCommandState();
    }

    private void RaiseCommandState()
    {
        if (AddBookCommand is RelayCommand add)
            add.RaiseCanExecuteChanged();

        if (EditBookCommand is RelayCommand edit)
            edit.RaiseCanExecuteChanged();

        if (ArchiveBookCommand is AsyncRelayCommand archive)
            archive.RaiseCanExecuteChanged();

        if (DeleteBookCommand is AsyncRelayCommand delete)
            delete.RaiseCanExecuteChanged();
    }

    private static string GetSummary<T>(IEnumerable<TriStateOption<T>> options)
    {
        var include = options.Count(x => x.State == CheckBoxState.Include);
        var exclude = options.Count(x => x.State == CheckBoxState.Exclude);

        return include + exclude == 0 ? "Любые >" : $"+{include} / -{exclude}";
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _appState.PropertyChanged -= AppStateOnPropertyChanged;
        _refreshDebounceCts?.Cancel();
        _refreshDebounceCts?.Dispose();
        _refreshGate.Dispose();
    }

    public sealed class Range<T> : ViewModelBase where T : struct, IComparable<T>
    {
        private T? _from;
        private T? _to;

        public T? From
        {
            get => _from;
            set
            {
                if (SetProperty(ref _from, value) && _to.HasValue && _from.HasValue && _from.Value.CompareTo(_to.Value) > 0)
                    To = _from;
            }
        }

        public T? To
        {
            get => _to;
            set
            {
                if (SetProperty(ref _to, value) && _from.HasValue && _to.HasValue && _to.Value.CompareTo(_from.Value) < 0)
                    From = _to;
            }
        }
    }

    public sealed class TriStateOption<T> : ViewModelBase
    {
        public TriStateOption(T value, string name)
        {
            Value = value;
            Name = name;
        }

        public T Value { get; }
        public string Name { get; }

        private CheckBoxState _state = CheckBoxState.Neutral;
        public CheckBoxState State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }
    }
}