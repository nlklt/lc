// EditBookViewModel.cs
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
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace lc.ViewModels
{
    public class EditBookViewModel : ViewModelBase
    {
        private static IChapterRepository ChapterRepository { get; } = new ChapterRepository();
        private static ICommentRepository CommentRepository { get; } = new CommentRepository();
        private static ITagRepository TagRepository { get; } = new TagRepository();
        private static ICategoryRepository CategoryRepository { get; } = new CategoryRepository();

        private static IBookRepository BookRepository { get; } =
            new BookRepository(ChapterRepository, CommentRepository, TagRepository, CategoryRepository);

        private readonly IBookService _bookService;
        private readonly INavigationService _navigation;
        private readonly AppState _appState;

        private Book? _originalBook;

        private int? _bookId;
        private bool _isInitialized;
        private bool _isBusy;
        private string _errorMessage = string.Empty;

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

        private DateTime _createdAt;
        private DateTime _updatedAt;

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
                    OnPropertyChanged(nameof(HeaderText));
                }
            }
        }

        public string HeaderText => IsEditMode ? "Редактирование книги..." : "Создание книги...";

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string Title
        {
            get => _title;
            set
            {
                if (SetProperty(ref _title, value))
                {
                    (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string AuthorName
        {
            get => _authorName;
            set => SetProperty(ref _authorName, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string CoverImagePath
        {
            get => _coverImagePath;
            set
            {
                if (SetProperty(ref _coverImagePath, value))
                {
                    OnPropertyChanged(nameof(HasCover));
                    OnPropertyChanged(nameof(CoverPath));
                }
            }
        }

        public bool HasCover => !string.IsNullOrWhiteSpace(CoverImagePath);

        public ImageSource? CoverPath => BuildCoverPath(CoverImagePath);

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

        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                if (SetProperty(ref _createdAt, value))
                {
                    OnPropertyChanged(nameof(CreatedAtText));
                }
            }
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set
            {
                if (SetProperty(ref _updatedAt, value))
                {
                    OnPropertyChanged(nameof(UpdatedAtText));
                }
            }
        }

        public string CreatedAtText => CreatedAt == default
            ? "—"
            : CreatedAt.ToString("dd.MM.yyyy HH:mm");

        public string UpdatedAtText => UpdatedAt == default
            ? "—"
            : UpdatedAt.ToString("dd.MM.yyyy HH:mm");

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
        public ICommand ChooseCoverCommand { get; }
        public ICommand ClearCoverCommand { get; }

        public EditBookViewModel(int? bookId = null)
        {
            _bookService = ServiceLocator.BookService;
            _navigation = ServiceLocator.NavigationService;
            _appState = ServiceLocator.AppState;

            BookId = bookId;

            InitializeCommand = new AsyncRelayCommand(_ => InitializeAsync());
            SaveCommand = new AsyncRelayCommand(_ => SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
            ChooseCoverCommand = new RelayCommand(_ => ChooseCover());
            ClearCoverCommand = new RelayCommand(_ => ClearCover());
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                await LoadLookupsAsync();

                if (BookId.HasValue)
                    await LoadBookAsync(BookId.Value);
                else
                    LoadDefaultsForCreate();

                _isInitialized = true;
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

        private void LoadDefaultsForCreate()
        {
            SelectedWritingStatus = WritingStatus.Анонс;
            SelectedLanguage = Language.Русский;
            SelectedAgeRating = 12;
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            SymbolsCount = 0;
            ChaptersCount = 0;
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
            var book = await BookRepository.GetByIdAsync(bookId);
            if (book == null)
                return;

            _originalBook = book.Clone();

            Title = book.Title ?? string.Empty;
            AuthorName = book.AuthorName ?? string.Empty;
            Description = book.Description ?? string.Empty;
            CoverImagePath = book.CoverImagePath ?? string.Empty;

            SelectedBookStatus = book.BookStatus;
            SelectedWritingStatus = book.WritingStatus;
            SelectedLanguage = book.Language;
            SelectedAgeRating = book.AgeRating;

            SymbolsCount = book.SymbolsCount;
            ChaptersCount = book.ChaptersCount;

            CreatedAt = book.CreatedAt;
            UpdatedAt = book.UpdatedAt;

            var categoryIds = book.Categories.Select(x => x.CategoryId).ToHashSet();
            var tagIds = book.Tags.Select(x => x.TagId).ToHashSet();

            foreach (var item in CategoryItems)
                item.IsSelected = categoryIds.Contains(item.Value.CategoryId);

            foreach (var item in TagItems)
                item.IsSelected = tagIds.Contains(item.Value.TagId);
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
                var selectedCategories = CategoryItems
                    .Where(x => x.IsSelected)
                    .Select(x => x.Value)
                    .ToList();

                var selectedTags = TagItems
                    .Where(x => x.IsSelected)
                    .Select(x => x.Value)
                    .ToList();

                var now = DateTime.Now;

                var book = new Book
                {
                    BookId = BookId ?? 0,
                    Title = Title.Trim(),
                    PublisherId = _originalBook?.PublisherId ?? _appState.CurrentUser?.UserId ?? 0,
                    AuthorName = string.IsNullOrWhiteSpace(AuthorName) ? null : AuthorName.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    CoverImagePath = string.IsNullOrWhiteSpace(CoverImagePath) ? null : CoverImagePath.Trim(),

                    Categories = selectedCategories,
                    Tags = selectedTags,

                    BookStatus = BookStatus.Published,  // !!!
                    WritingStatus = SelectedWritingStatus,
                    Language = SelectedLanguage,
                    AgeRating = SelectedAgeRating,

                    SymbolsCount = SymbolsCount,
                    ChaptersCount = ChaptersCount,

                    Views = _originalBook?.Views ?? 0,
                    Rating = _originalBook?.Rating ?? 0,

                    CreatedAt = _originalBook?.CreatedAt ?? now,
                    UpdatedAt = now,

                    Chapters = _originalBook?.Chapters?.ToList() ?? new(),
                    Comments = _originalBook?.Comments?.ToList() ?? new()
                };

                if (IsEditMode)
                    await BookRepository.UpdateAsync(book);
                else
                    await BookRepository.CreateAsync(book);

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

        private void ChooseCover()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выбор обложки",
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp|All files|*.*",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                CoverImagePath = dialog.FileName;
            }
        }

        private void ClearCover()
        {
            CoverImagePath = string.Empty;
        }

        private void Cancel()
        {
            _navigation.Navigate(new CatalogViewModel());
        }

        private static ImageSource? BuildCoverPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
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