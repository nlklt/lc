using lc.Commands;
using lc.Data.Repositories;
using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Infrastructure.Repositories.Sql;
using lc.Models;
using lc.Models.Enums;
using lc.Services;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace lc.ViewModels
{
    public class EditBookViewModel : ViewModelBase
    {
        private static IChapterRepository ChapterRepository { get; } = new ChapterRepository();
        private static ICommentRepository CommentRepository { get; } = new CommentRepository();
        private static ITagRepository TagRepository { get; } = new TagRepository();
        private static ICategoryRepository CategoryRepository { get; } = new CategoryRepository();
        private static IBookRepository _bookrepositor { get; } = new BookRepository(ChapterRepository, CommentRepository, TagRepository, CategoryRepository);

        private readonly IBookService _bookService;
        private readonly INavigationService _navigation;
        private readonly AppState _appState;

        private int? _bookId;
        private bool _isInitialized;
        private bool _isBusy;
        private string _errorMessage = string.Empty;

        private int _publisherId;
        private string _title = string.Empty;
        private string _authorName = string.Empty;
        private string _description = string.Empty;
        private string _coverImagePath = string.Empty;

        private BookStatus _selectedBookStatus;
        private WritingStatus _selectedWritingStatus;
        private Language _selectedLanguage;
        private int _selectedAgeRating = 12;

        private int _symbolsCount;
        private int _chaptersCount;

        private ObservableCollection<SelectableItem<Category>> _categoryItems = new();
        private ObservableCollection<SelectableItem<Tag>> _tagItems = new();

        public bool IsEditMode => BookId.HasValue;
        public int? BookId
        {
            get => _bookId;
            private set
            {
                if (SetProperty(ref _bookId, value))
                {
                    OnPropertyChanged(nameof(IsEditMode));
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string AuthorName
        {
            get => _authorName;
            set => SetProperty(ref _authorName, value);
        }

        public int PublisherId
        {
            get => _publisherId;
            set => SetProperty(ref _publisherId, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string CoverImagePath
        {
            get => _coverImagePath;
            set => SetProperty(ref _coverImagePath, value);
        }

        public BookStatus SelectedBookStatus
        {
            get => _selectedBookStatus;
            set => SetProperty(ref _selectedBookStatus, value);
        }

        public WritingStatus SelectedWritingStatus
        {
            get => _selectedWritingStatus;
            set => SetProperty(ref _selectedWritingStatus, value);
        }

        public Language SelectedLanguage
        {
            get => _selectedLanguage;
            set => SetProperty(ref _selectedLanguage, value);
        }

        public int SelectedAgeRating
        {
            get => _selectedAgeRating;
            set => SetProperty(ref _selectedAgeRating, value);
        }

        public int SymbolsCount
        {
            get => _symbolsCount;
            set => SetProperty(ref _symbolsCount, value);
        }

        public int ChaptersCount
        {
            get => _chaptersCount;
            set => SetProperty(ref _chaptersCount, value);
        }

        public ObservableCollection<SelectableItem<Category>> CategoryItems
        {
            get => _categoryItems;
            private set => SetProperty(ref _categoryItems, value);
        }

        public ObservableCollection<SelectableItem<Tag>> TagItems
        {
            get => _tagItems;
            private set => SetProperty(ref _tagItems, value);
        }

        public ObservableCollection<BookStatus> BookStatuses { get; } =
            new(Enum.GetValues<BookStatus>());

        public ObservableCollection<WritingStatus> WritingStatuses { get; } =
            new(Enum.GetValues<WritingStatus>());

        public ObservableCollection<Language> Languages { get; } =
            new(Enum.GetValues<Language>());

        public ObservableCollection<int> AgeRatings { get; } =
            new() { 3, 6, 12, 16, 18 };

        public ICommand InitializeCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditBookViewModel(int? bookId = null)
        {
            _bookService = ServiceLocator.BookService;
            _navigation = ServiceLocator.NavigationService;
            _appState = ServiceLocator.AppState;

            BookId = bookId;

            InitializeCommand = new AsyncRelayCommand(_ => InitializeAsync());
            SaveCommand = new AsyncRelayCommand(_ => SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());

            HookChangeTracking();
        }

        private void HookChangeTracking()
        {
            PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(Title) or nameof(AuthorName) or nameof(PublisherId) or
                    nameof(Description) or nameof(CoverImagePath) or nameof(SelectedBookStatus) or
                    nameof(SelectedWritingStatus) or nameof(SelectedLanguage) or nameof(SelectedAgeRating) or
                    nameof(SymbolsCount) or nameof(ChaptersCount))
                {
                    (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                }
            };
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            IsBusy = true;
            try
            {
                await LoadLookupsAsync();

                if (BookId.HasValue)
                    await LoadBookAsync(BookId.Value);

                _isInitialized = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadLookupsAsync()
        {
            var categories = await _bookService.GetAllCategoriesAsync();
            var tags = await _bookService.GetAllTagsAsync();

            CategoryItems = new ObservableCollection<SelectableItem<Category>>(
                categories.Select(x => new SelectableItem<Category>(x, x.Name)));

            TagItems = new ObservableCollection<SelectableItem<Tag>>(
                tags.Select(x => new SelectableItem<Tag>(x, x.Name)));
        }

        private async Task LoadBookAsync(int bookId)
        {
            var book = await _bookrepositor.GetByIdAsync(bookId);
            if (book == null)
                return;

            Title = book.Title ?? string.Empty;
            AuthorName = book.AuthorName ?? string.Empty;
            PublisherId = book.PublisherId;
            Description = book.Description ?? string.Empty;
            CoverImagePath = book.CoverImagePath ?? string.Empty;
            SelectedBookStatus = book.BookStatus;
            SelectedWritingStatus = book.WritingStatus;
            SelectedLanguage = book.Language;
            SelectedAgeRating = book.AgeRating;
            SymbolsCount = book.SymbolsCount;
            ChaptersCount = book.ChaptersCount;

            //var categoryIds = await _bookrepositor.GetBookCategoryIdsAsync(bookId);
            //var tagIds = await _bookrepositor.GetBookTagIdsAsync(bookId);

            //foreach (var item in CategoryItems)
            //    item.IsSelected = categoryIds.Contains(item.Value.CategoryId);

            //foreach (var item in TagItems)
            //    item.IsSelected = tagIds.Contains(item.Value.TagId);
        }

        private bool CanSave()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(Title);
        }

        private async Task SaveAsync()
        {
            if (!CanSave())
                return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var selectedCategoryIds = CategoryItems
                    .Where(x => x.IsSelected)
                    .Select(x => x.Value.CategoryId)
                    .ToList();

                var selectedTagIds = TagItems
                    .Where(x => x.IsSelected)
                    .Select(x => x.Value.TagId)
                    .ToList();

                var book = new Book
                {
                    BookId = BookId ?? 0,
                    Title = Title.Trim(),
                    AuthorName = string.IsNullOrWhiteSpace(AuthorName) ? null : AuthorName.Trim(),
                    PublisherId = _appState.CurrentUser.UserId,
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    CoverImagePath = string.IsNullOrWhiteSpace(CoverImagePath) ? null : CoverImagePath.Trim(),
                    BookStatus = SelectedBookStatus,
                    WritingStatus = SelectedWritingStatus,
                    Language = SelectedLanguage,
                    AgeRating = SelectedAgeRating,
                    SymbolsCount = SymbolsCount,
                    ChaptersCount = ChaptersCount
                };

                if (IsEditMode)
                {
                    await _bookrepositor.UpdateAsync(book);
                }
                else
                {
                    await _bookrepositor.CreateAsync(book);
                }

                _navigation.Navigate(new CatalogViewModel());
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Cancel()
        {
            _navigation.Navigate(new CatalogViewModel());
        }

        public class SelectableItem<T> : ViewModelBase
        {
            public T Value { get; }
            public string Name { get; }

            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set => SetProperty(ref _isSelected, value);
            }

            public SelectableItem(T value, string name)
            {
                Value = value;
                Name = name;
            }
        }
    }
}