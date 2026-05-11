using lc.Commands;
using lc.Infrastructure;
using lc.Models;
using lc.Models.Enums;
using lc.Services;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using Microsoft.IdentityModel.Tokens;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace lc.ViewModels
{
    public class CatalogViewModel : ViewModelBase
    {
        private readonly AppState            _appState;
        private readonly IBookService        _bookService;
        private readonly IDialogService      _dialogService;
        private readonly INavigationService  _navigationService;
        private readonly IUserLibraryService _userLibraryService;

        private string _searchText = string.Empty;
        private string _selectedSortField = nameof(BookListItem.Title);
        private bool   _sortAscending = true;
        private bool   _isLoading;
        private bool   _suppressAutoRefresh;
        private bool   _isInitialized;
        
        private ObservableCollection<TriStateOption<Category>>      _categoryOptions      = new();
        private ObservableCollection<TriStateOption<Tag>>           _tagOptions           = new();
        private ObservableCollection<TriStateOption<BookStatus>>    _bookStatusOptions    = new();
        private ObservableCollection<TriStateOption<WritingStatus>> _writingStatusOptions = new();
        private ObservableCollection<TriStateOption<Language>>      _languageOptions      = new();
        private ObservableCollection<TriStateOption<int>>           _ageRatingOptions     = new();

        private ObservableCollection<BookListItem> _books = [];
        public ObservableCollection<BookListItem> Books { get => _books; set => SetProperty(ref _books, value); }
        
        private BookListItem?                      _selectedBook;
        public BookListItem? SelectedBook
        {
            get => _selectedBook;
            set
            {
                SetProperty(ref _selectedBook, value);
                _appState.SelectedBook = SelectedBook;
                OnPropertyChanged(nameof(CanEditSelectedBook));
                OnPropertyChanged(nameof(CanDeleteSelectedBook));
            }
        }
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    OnFilterChanged();
            }
        }
        public string SelectedSortField
        {
            get => _selectedSortField;
            set
            {
                if (SetProperty(ref _selectedSortField, value))
                    OnFilterChanged();
            }
        }
        public bool SortAscending
        {
            get => _sortAscending;
            set
            {
                if (SetProperty(ref _sortAscending, value))
                    OnFilterChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isStrictCategoryMatch;
        public bool IsStrictCategoryMatch
        {
            get => _isStrictCategoryMatch;
            set => SetProperty(ref _isStrictCategoryMatch, value);
        }
        private bool _isStrictTagMatch;
        public bool IsStrictTagMatch
        {
            get => _isStrictTagMatch;
            set => SetProperty(ref _isStrictTagMatch, value);
        }
        private bool _isCategoriesOpen;
        public bool IsCategoriesOpen
        {
            get => _isCategoriesOpen;
            set { _isCategoriesOpen = value; OnPropertyChanged(); }
        }
        private bool _isTagsOpen;
        public bool IsTagsOpen
        {
            get => _isTagsOpen;
            set { _isTagsOpen = value; OnPropertyChanged(); }
        }
        public string CategoriesSummaryText => GetSummary(CategoryOptions);
        public string TagsSummaryText => GetSummary(TagOptions);

        private string GetSummary<T>(IEnumerable<TriStateOption<T>> options)
        {
            int inc = options.Count(x => x.State == CheckBoxState.Include);
            int exc = options.Count(x => x.State == CheckBoxState.Exclude);
            return (inc + exc) == 0 ? "Любые >" : $"+{inc} / -{exc}";
        }

        public bool IsAdmin => _appState.IsAdmin;
        public bool IsAuthenticated => _appState.IsAuthenticated;
        public bool CanManageBooks => _appState.CanManageBooks;
        public bool CanEditSelectedBook => CanManageBooks && SelectedBook != null;
        public bool CanDeleteSelectedBook => CanManageBooks && SelectedBook != null;

        public Range<double>    Rating { get; } = new();
        public Range<int>       Chapters { get; } = new();
        public Range<int>       Symbols { get; } = new();
        public Range<DateTime>  CreatedAt { get; } = new();

        public ObservableCollection<TriStateOption<Category>> CategoryOptions { get => _categoryOptions; private set => SetProperty(ref _categoryOptions, value); }
        public ObservableCollection<TriStateOption<Tag>> TagOptions { get => _tagOptions; private set => SetProperty(ref _tagOptions, value); }
        public ObservableCollection<TriStateOption<BookStatus>> BookStatusOptions { get => _bookStatusOptions; private set => SetProperty(ref _bookStatusOptions, value); }
        public ObservableCollection<TriStateOption<WritingStatus>> WritingStatusOptions { get => _writingStatusOptions; private set => SetProperty(ref _writingStatusOptions, value); }
        public ObservableCollection<TriStateOption<Language>> LanguageOptions { get => _languageOptions; private set => SetProperty(ref _languageOptions, value); }
        public ObservableCollection<TriStateOption<int>> AgeRatingOptions { get => _ageRatingOptions; private set => SetProperty(ref _ageRatingOptions, value); }

        public ObservableCollection<string> SortFields { get; } =
        [
            nameof(BookListItem.Title),
            nameof(BookListItem.AuthorName),
            nameof(BookListItem.PublisherName),
            nameof(BookListItem.CreatedAt),
            nameof(BookListItem.Rating),
            nameof(BookListItem.Views),
            nameof(BookListItem.ChaptersCount),
            nameof(BookListItem.SymbolsCount),
            nameof(BookListItem.BookStatus),
            nameof(BookListItem.WritingStatus)
        ];

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
        public ICommand DeleteBookCommand { get; }

        public CatalogViewModel()
        {
            _appState = ServiceLocator.AppState;
            _bookService = ServiceLocator.BookService;
            _userLibraryService = ServiceLocator.UserLibraryService;
            _navigationService = ServiceLocator.NavigationService;
            _dialogService = ServiceLocator.DialogService;

            InitializeCommand =       new AsyncRelayCommand(_ => InitializeAsync());
            ToggleCategoriesCommand = new RelayCommand(_ => ToggleCategories());
            ToggleTagsCommand =       new RelayCommand(_ => ToggleTags());
            ApplyFiltersCommand =     new AsyncRelayCommand(_ => ApplyFiltersAsync());
            ResetFiltersCommand =     new AsyncRelayCommand(_ => ResetFiltersAsync());

            OpenDetailsCommand =  new RelayCommand(OpenDetails);
            StartReadingCommand = new AsyncRelayCommand(StartReadingAsync);
            AddToListCommand =    new AsyncRelayCommand(AddToListAsync);

            AddBookCommand =    new RelayCommand(_ => AddBook());
            EditBookCommand =   new RelayCommand(EditBook);
            DeleteBookCommand = new AsyncRelayCommand(DeleteBookAsync);

            HookAppState();
            LoadStaticFilterOptions();
        }

        private void HookAppState()
        {
            _appState.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppState.CurrentUser))
                {
                    OnPropertyChanged(nameof(IsAdmin));
                    OnPropertyChanged(nameof(IsAuthenticated));
                    OnPropertyChanged(nameof(CanManageBooks));
                    OnPropertyChanged(nameof(CanEditSelectedBook));
                    OnPropertyChanged(nameof(CanDeleteSelectedBook));
                }
            };
        }

        private void HookOptionAutoRefresh<T>(ObservableCollection<TriStateOption<T>> options)
        {
            foreach (var option in options)
                option.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(TriStateOption<T>.State))
                    {
                        OnPropertyChanged(nameof(CategoriesSummaryText));
                        OnPropertyChanged(nameof(TagsSummaryText));
                    }
                };
        }

        private void OnFilterChanged()
        {
            if (!_isInitialized || _suppressAutoRefresh)
                return;

            _ = ApplyFiltersAsync();
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            IsLoading = true;
            try
            {
                await LoadLookupOptionsAsync();
                await ApplyFiltersAsync();
                _isInitialized = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadStaticFilterOptions()
        {
            BookStatusOptions = new ObservableCollection<TriStateOption<BookStatus>>( Enum.GetValues<BookStatus>().Select(x => new TriStateOption<BookStatus>(x, x.ToString())) );
            WritingStatusOptions = new ObservableCollection<TriStateOption<WritingStatus>>( Enum.GetValues<WritingStatus>().Select(x => new TriStateOption<WritingStatus>(x, x.ToString())) );
            LanguageOptions = new ObservableCollection<TriStateOption<Language>>( Enum.GetValues<Language>().Select(x => new TriStateOption<Language>(x, x.ToString())) );
            AgeRatingOptions = new ObservableCollection<TriStateOption<int>> { new(3, "3+"), new(6, "6+"), new(12, "12+"), new(16, "16+"), new(18, "18+") };

            HookOptionAutoRefresh(BookStatusOptions);
            HookOptionAutoRefresh(WritingStatusOptions);
            HookOptionAutoRefresh(LanguageOptions);
            HookOptionAutoRefresh(AgeRatingOptions);
        }

        private async Task LoadLookupOptionsAsync()
        {
            var categories = await _bookService.GetAllCategoriesAsync();
            var tags = await _bookService.GetAllTagsAsync();

            CategoryOptions = new ObservableCollection<TriStateOption<Category>>(
                categories.Select(x => new TriStateOption<Category>(x, x.Name)));

            TagOptions = new ObservableCollection<TriStateOption<Tag>>(
                tags.Select(x => new TriStateOption<Tag>(x, x.Name)));

            HookOptionAutoRefresh(CategoryOptions);
            HookOptionAutoRefresh(TagOptions);
        }

        private void ToggleCategories()
        {
            IsCategoriesOpen = !IsCategoriesOpen;
        }

        private void ToggleTags()
        {
            IsTagsOpen = !IsTagsOpen;
        }
        private async Task ApplyFiltersAsync()
        {
            if (!_isInitialized && Books.Count == 0)
            {
                // Разрешаем первичную загрузку до флага _isInitialized.
            }

            IsLoading = true;
            try
            {
                var criteria = BuildCriteria();
                var items = await _bookService.GetCatalogAsync(criteria);
                Books = new ObservableCollection<BookListItem>(items);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private BookFilterCriteria BuildCriteria()
        {
            return new BookFilterCriteria
            {
                SearchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                
                IncludeTagIds = [.. TagOptions.Where(x => x.State == CheckBoxState.Include).Select(x => x.Value.TagId)],
                ExcludeTagIds = [.. TagOptions.Where(x => x.State == CheckBoxState.Exclude).Select(x => x.Value.TagId)],
                
                IncludeCategoryIds = [.. CategoryOptions.Where(x => x.State == CheckBoxState.Include).Select(x => x.Value.CategoryId)],
                ExcludeCategoryIds = [.. CategoryOptions.Where(x => x.State == CheckBoxState.Exclude).Select(x => x.Value.CategoryId)],
                
                StrictTagMatch = this.IsStrictTagMatch,
                StrictCategoryMatch = this.IsStrictCategoryMatch,

                IncludeBookStatuses = [.. BookStatusOptions.Where(x => x.State == CheckBoxState.Include).Select(x => x.Value)],
                ExcludeBookStatuses = [.. BookStatusOptions.Where(x => x.State == CheckBoxState.Exclude).Select(x => x.Value)],

                IncludeWritingStatuses = [.. WritingStatusOptions.Where(x => x.State == CheckBoxState.Include).Select(x => x.Value)],
                ExcludeWritingStatuses = [.. WritingStatusOptions.Where(x => x.State == CheckBoxState.Exclude).Select(x => x.Value)],

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

                SortField = SelectedSortField,
                SortAscending = SortAscending
            };
        }

        private void AddBook()
        {
            _navigationService.Navigate(new EditBookViewModel());
        }
        private void EditBook(object? parameter)
        {
            var book = parameter as BookListItem ?? SelectedBook;
            if (book == null)
                return;

            _navigationService.Navigate(new EditBookViewModel(book.BookId));
        }
        private async Task DeleteBookAsync(object? parameter)
        {
            var book = parameter as BookListItem ?? SelectedBook;
            if (book == null)
                return;

            var confirmed = await _dialogService.ConfirmAsync(
                "Удалить книгу",
                $"Удалить книгу «{book.Title}»?");

            if (!confirmed)
                return;

            await _bookService.DeleteBookAsync(book.BookId);

            if (SelectedBook?.BookId == book.BookId)
                SelectedBook = null;

            await ApplyFiltersAsync();
        }
        private async Task AddToListAsync(object? parameter)
        {
            var book = parameter as BookListItem ?? SelectedBook;
            if (book == null)
                return;

            if (_appState.CurrentUser == null)
                return;

            var lists = await _userLibraryService.GetListsAsync(_appState.CurrentUser.UserId);

            var selectedList = await _dialogService.ChooseListAsync(
                "Добавить в список",
                "Выберите личный список:",
                lists.Select(x => x.Name).ToList());

            if (string.IsNullOrWhiteSpace(selectedList))
                return;

            var list = lists.FirstOrDefault(x => x.Name == selectedList);
            if (list == null)
                return;

            await _userLibraryService.AddBookToListAsync(
                _appState.CurrentUser.UserId,
                list.ListId,
                book.BookId);
        }

        private void OpenDetails(object? parameter)
        {
            var book = parameter as BookListItem ?? SelectedBook;
            if (book == null)
                return;

            _navigationService.Navigate(new BookDetailsViewModel(book.BookId));
        }

        private async Task StartReadingAsync(object? parameter)
        {
            var book = parameter as BookListItem ?? SelectedBook;
            if (book == null)
                return;

            _appState.SelectedBook = book;
            _navigationService.Navigate(new ReaderViewModel(book.BookId));
        }

        private async Task ResetFiltersAsync()
        {
            _suppressAutoRefresh = true;
            try
            {
                SearchText = string.Empty;
                SelectedSortField = nameof(BookListItem.Title);
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
                ClearTriState(BookStatusOptions);
                ClearTriState(WritingStatusOptions);
                ClearTriState(LanguageOptions);
                ClearTriState(AgeRatingOptions);
            }
            finally
            {
                _suppressAutoRefresh = false;
            }

            await ApplyFiltersAsync();
        }

        private static void ClearTriState<T>(ObservableCollection<TriStateOption<T>> options)
        {
            foreach (var opt in options)
                opt.State = CheckBoxState.Neutral;
        }
    }

    public class Range<T> : ViewModelBase where T : struct, IComparable<T>
    {
        private T? _from;
        public T? From
        {
            get => _from;
            set => SetProperty(ref _from, value);
        }

        private T? _to;
        public T? To
        {
            get => _to;
            set => SetProperty(ref _to, value);
        }
    }

    public class TriStateOption<T> : ViewModelBase
    {
        public T Value { get; }
        public string Name { get; }

        private CheckBoxState _state = CheckBoxState.Neutral;
        public CheckBoxState State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        public TriStateOption(T value, string name)
        {
            Value = value;
            Name = name;
        }
    }
}
