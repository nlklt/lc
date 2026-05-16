using lc.Commands;
using lc.Infrastructure;
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace lc.ViewModels;

public sealed class EditBookViewModel : ViewModelBase
{
    private const int MaxTitleLength = 32;
    private const int MaxAuthorLength = 16;
    private const int MaxDescriptionLength = 1_000;

    private static readonly string[] AllowedCoverExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".webp"];

    private readonly IBookService _bookService;
    private readonly INavigationService _navigation;
    private readonly AppState _appState;
    private readonly SemaphoreSlim _initGate = new(1, 1);

    private Book? _originalBook;
    private int? _bookId;
    private bool _isInitialized;
    private bool _isInitializing;
    private bool _isBusy;
    private string _errorMessage = string.Empty;

    private string _title = string.Empty;
    private string _authorName = string.Empty;
    private string _description = string.Empty;
    private string _coverImagePath = string.Empty;

    private WritingStatus _selectedWritingStatus;
    private Language _selectedLanguage;
    private int _selectedAgeRating = 12;

    private long _symbolsCount;
    private int _chaptersCount;
    private DateTime _createdAt;
    private DateTime _updatedAt;
    private BookStatus _bookStatus = BookStatus.Draft;

    private ObservableCollection<SelectableItem<Category>> _categoryItems = [];
    private ObservableCollection<SelectableItem<Tag>> _tagItems = [];

    private HashSet<int> _originalCategoryIds = [];
    private HashSet<int> _originalTagIds = [];

    public EditBookViewModel(
        IBookService bookService,
        INavigationService navigation,
        AppState appState,
        int? bookId = null)
    {
        _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));

        BookId = bookId;

        InitializeCommand = new AsyncRelayCommand(_ => InitializeAsync(), _ => !IsBusy && !_isInitialized);
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync(), _ => CanSave);
        PublishCommand = new AsyncRelayCommand(_ => PublishAsync(), _ => CanPublish);
        CancelCommand = new RelayCommand(_ => Cancel(), _ => !IsBusy);
        ChooseCoverCommand = new RelayCommand(_ => ChooseCover(), _ => !IsBusy);
        ClearCoverCommand = new RelayCommand(_ => ClearCover(), _ => !IsBusy && HasCover);

        _ = InitializeAsync();
    }

    public bool IsEditMode => BookId.HasValue;

    public bool IsPublishedEditMode => IsEditMode && _originalBook?.BookStatus == BookStatus.Published;

    public int? BookId
    {
        get => _bookId;
        private set
        {
            if (SetProperty(ref _bookId, value))
            {
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(IsPublishedEditMode));
                OnPropertyChanged(nameof(HeaderText));
                OnPropertyChanged(nameof(PrimaryActionText));
                OnPropertyChanged(nameof(ShowPublishAction));
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(CanPublish));
            }
        }
    }

    public string HeaderText => IsEditMode ? "Редактирование книги..." : "Создание книги...";

    public string PrimaryActionText => IsPublishedEditMode
        ? "Сохранить"
        : IsEditMode
            ? "Сохранить черновик"
            : "Создать черновик";

    public bool ShowPublishAction => !IsPublishedEditMode;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                RefreshCommands();
        }
    }

    public bool IsInitialized => _isInitialized;

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
            var normalized = value ?? string.Empty;
            if (SetProperty(ref _title, normalized))
                OnBookChanged();
        }
    }

    public string AuthorName
    {
        get => _authorName;
        set
        {
            var normalized = value ?? string.Empty;
            if (SetProperty(ref _authorName, normalized))
                OnBookChanged();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            var normalized = value ?? string.Empty;
            if (SetProperty(ref _description, normalized))
                OnBookChanged();
        }
    }

    public string CoverImagePath
    {
        get => _coverImagePath;
        set
        {
            var normalized = value ?? string.Empty;
            if (SetProperty(ref _coverImagePath, normalized))
            {
                OnPropertyChanged(nameof(HasCover));
                OnPropertyChanged(nameof(CoverPath));
                OnBookChanged();
            }
        }
    }

    public bool HasCover => !string.IsNullOrWhiteSpace(CoverImagePath);

    public ImageSource? CoverPath => BuildCoverPath(CoverImagePath);

    public WritingStatus SelectedWritingStatus
    {
        get => _selectedWritingStatus;
        set
        {
            if (SetProperty(ref _selectedWritingStatus, value))
                OnBookChanged();
        }
    }

    public Language SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value))
                OnBookChanged();
        }
    }

    public int SelectedAgeRating
    {
        get => _selectedAgeRating;
        set
        {
            if (SetProperty(ref _selectedAgeRating, value))
                OnBookChanged();
        }
    }

    public BookStatus SelectedBookStatus => _bookStatus;

    public string CreatedAtText =>
        CreatedAt == default ? "-" : CreatedAt.ToString("dd.MM.yyyy");

    public string UpdatedAtText =>
        UpdatedAt == default ? "-" : UpdatedAt.ToString("dd.MM.yyyy");

    public long SymbolsCount
    {
        get => _symbolsCount;
        private set => SetProperty(ref _symbolsCount, value);
    }

    public int ChaptersCount
    {
        get => _chaptersCount;
        private set => SetProperty(ref _chaptersCount, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        private set => SetProperty(ref _createdAt, value);
    }

    public DateTime UpdatedAt
    {
        get => _updatedAt;
        private set => SetProperty(ref _updatedAt, value);
    }

    public string BookStatusText => GetBookStatusText(SelectedBookStatus);

    public string LanguageText => GetLanguageText(SelectedLanguage);

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

    public ObservableCollection<WritingStatus> WritingStatuses { get; } = new(Enum.GetValues<WritingStatus>());
    public ObservableCollection<Language> Languages { get; } = new(Enum.GetValues<Language>());
    public ObservableCollection<int> AgeRatings { get; } = [3, 6, 12, 16, 18];

    public ICommand InitializeCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand PublishCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ChooseCoverCommand { get; }
    public ICommand ClearCoverCommand { get; }

    public bool CanSave =>
        !IsBusy &&
        _isInitialized &&
        IsValid();

    public bool CanPublish =>
        !IsBusy &&
        _isInitialized &&
        !IsPublishedEditMode &&
        IsValid();

    public async Task InitializeAsync()
    {
        if (_isInitialized || _isInitializing)
            return;

        await _initGate.WaitAsync();
        try
        {
            if (_isInitialized)
                return;

            _isInitializing = true;
            IsBusy = true;
            ErrorMessage = string.Empty;

            await LoadLookupsAsync();

            bool loaded = true;

            if (BookId.HasValue)
            {
                loaded = await LoadBookAsync(BookId.Value);
            }
            else
            {
                LoadDefaultsForCreate();
            }

            if (!loaded)
                return;

            _isInitialized = true;
            RefreshStateProperties();
            RefreshCommands();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка инициализации формы книги: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _isInitializing = false;
            _initGate.Release();
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

    private async Task<bool> LoadBookAsync(int bookId)
    {
        var book = await _bookService.GetBookByIdAsync(bookId);
        if (book is null)
        {
            ErrorMessage = "Книга не найдена.";
            return false;
        }

        if (!CanCurrentUserEdit(book))
        {
            ErrorMessage = "Недостаточно прав для редактирования книги.";
            return false;
        }

        _originalBook = book.Clone();

        Title = book.Title;
        AuthorName = book.AuthorName ?? string.Empty;
        Description = book.Description ?? string.Empty;
        CoverImagePath = book.CoverImagePath ?? string.Empty;

        SelectedWritingStatus = book.WritingStatus;
        SelectedLanguage = book.Language;
        SelectedAgeRating = book.AgeRating;

        SymbolsCount = book.SymbolsCount;
        ChaptersCount = book.ChaptersCount;
        CreatedAt = book.CreatedAt;
        UpdatedAt = book.UpdatedAt;
        _bookStatus = book.BookStatus;

        _originalCategoryIds = book.Categories.Select(x => x.CategoryId).ToHashSet();
        _originalTagIds = book.Tags.Select(x => x.TagId).ToHashSet();

        foreach (var item in CategoryItems)
            item.IsSelected = _originalCategoryIds.Contains(item.Value.CategoryId);

        foreach (var item in TagItems)
            item.IsSelected = _originalTagIds.Contains(item.Value.TagId);

        RefreshStateProperties();
        return true;
    }

    private void LoadDefaultsForCreate()
    {
        _originalBook = null;
        _originalCategoryIds = [];
        _originalTagIds = [];

        SelectedWritingStatus = WritingStatus.Анонс;
        SelectedLanguage = Language.Русский;
        SelectedAgeRating = 12;

        CreatedAt = DateTime.Now;
        UpdatedAt = CreatedAt;
        _bookStatus = BookStatus.Draft;
    }

    private bool CanCurrentUserEdit(Book book)
    {
        var user = _appState.CurrentUser;
        if (user is null)
            return false;

        if (book.BookStatus == BookStatus.Archived)
            return _appState.IsAdmin;

        return _appState.IsAdmin || user.UserId == book.PublisherId;
    }

    private bool IsValid()
    {
        var title = Title.Trim();
        var author = AuthorName.Trim();
        var description = Description.Trim();

        if (string.IsNullOrWhiteSpace(title) || title.Length > MaxTitleLength)
            return false;

        if (author.Length > MaxAuthorLength)
            return false;

        if (description.Length > MaxDescriptionLength)
            return false;

        if (SelectedAgeRating is not (3 or 6 or 12 or 16 or 18))
            return false;

        if (!Enum.IsDefined(typeof(WritingStatus), SelectedWritingStatus))
            return false;

        if (!Enum.IsDefined(typeof(Language), SelectedLanguage))
            return false;

        if (!string.IsNullOrWhiteSpace(CoverImagePath) && !IsValidCoverImagePath(CoverImagePath))
            return false;

        return true;
    }

    private void OnBookChanged()
    {
        RefreshCommands();
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanPublish));
        OnPropertyChanged(nameof(BookStatusText));
        OnPropertyChanged(nameof(LanguageText));
        OnPropertyChanged(nameof(PrimaryActionText));
    }

    private void RefreshStateProperties()
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(IsPublishedEditMode));
        OnPropertyChanged(nameof(HeaderText));
        OnPropertyChanged(nameof(PrimaryActionText));
        OnPropertyChanged(nameof(ShowPublishAction));
        OnPropertyChanged(nameof(BookStatusText));
        OnPropertyChanged(nameof(CreatedAtText));
        OnPropertyChanged(nameof(UpdatedAtText));
        OnPropertyChanged(nameof(LanguageText));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanPublish));
    }

    private async Task SaveAsync()
    {
        await CommitAsync(IsPublishedEditMode ? (_originalBook?.BookStatus ?? BookStatus.Draft) : BookStatus.Draft);
    }

    private async Task PublishAsync()
    {
        await CommitAsync(BookStatus.Published);
    }

    private async Task CommitAsync(BookStatus targetStatus)
    {
        if (!CanSave && targetStatus == BookStatus.Draft)
            return;

        if (!CanPublish && targetStatus == BookStatus.Published)
            return;

        if (_appState.CurrentUser is null)
        {
            ErrorMessage = "Пользователь не авторизован.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var now = DateTime.Now;
            var book = new Book
            {
                BookId = BookId ?? 0,
                Title = Title.Trim(),
                PublisherId = _originalBook?.PublisherId ?? _appState.CurrentUser.UserId,
                AuthorName = string.IsNullOrWhiteSpace(AuthorName) ? null : AuthorName.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                CoverImagePath = string.IsNullOrWhiteSpace(CoverImagePath) ? null : CoverImagePath.Trim(),
                Tags = TagItems.Where(x => x.IsSelected).Select(x => x.Value).ToList(),
                Categories = CategoryItems.Where(x => x.IsSelected).Select(x => x.Value).ToList(),
                WritingStatus = SelectedWritingStatus,
                Language = SelectedLanguage,
                AgeRating = SelectedAgeRating,
                SymbolsCount = _originalBook?.SymbolsCount ?? 0,
                ChaptersCount = _originalBook?.ChaptersCount ?? 0,
                Views = _originalBook?.Views ?? 0,
                Rating = _originalBook?.Rating ?? 0,
                CreatedAt = _originalBook?.CreatedAt ?? now,
                UpdatedAt = now,
                BookStatus = targetStatus
            };

            int savedBookId;

            if (IsEditMode)
            {
                await _bookService.UpdateBookAsync(book);
                savedBookId = book.BookId;
            }
            else
            {
                savedBookId = await _bookService.CreateBookAsync(book);
            }

            BookId = savedBookId;
            _bookStatus = targetStatus;
            RefreshStateProperties();

            _navigation.NavigateTo<BookDetailsViewModel>(savedBookId);
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
            Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp;*.webp|Все файлы|*.*",
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false,
            RestoreDirectory = true
        };

        if (dialog.ShowDialog() == true)
            CoverImagePath = dialog.FileName;
    }

    private void ClearCover()
    {
        CoverImagePath = string.Empty;
    }

    private void Cancel()
    {
        if (BookId is not null) _navigation.NavigateTo<BookDetailsViewModel>(BookId);
        else _navigation.NavigateTo<ProfileViewModel>();
    }

    private void RefreshCommands()
    {
        if (SaveCommand is AsyncRelayCommand save)
            save.RaiseCanExecuteChanged();

        if (PublishCommand is AsyncRelayCommand publish)
            publish.RaiseCanExecuteChanged();

        if (CancelCommand is RelayCommand cancel)
            cancel.RaiseCanExecuteChanged();

        if (ChooseCoverCommand is RelayCommand choose)
            choose.RaiseCanExecuteChanged();

        if (ClearCoverCommand is RelayCommand clear)
            clear.RaiseCanExecuteChanged();
    }

    private static bool IsValidCoverImagePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (!File.Exists(path))
            return false;

        var extension = Path.GetExtension(path);
        return AllowedCoverExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static ImageSource? BuildCoverPath(string? path)
    {
        if (!IsValidCoverImagePath(path ?? string.Empty))
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static string GetBookStatusText(BookStatus status) => status switch
    {
        BookStatus.Draft => "Черновик",
        BookStatus.Published => "Опубликована",
        BookStatus.Archived => "В архиве",
        _ => status.ToString()
    };

    private static string GetLanguageText(Language language) => language switch
    {
        Language.Русский => "Русский",
        Language.Английский => "Английский",
        Language.Немецкий => "Немецкий",
        Language.Китайский => "Китайский",
        Language.Испанский => "Испанский",
        _ => language.ToString()
    };

    public sealed class SelectableItem<T> : ViewModelBase
    {
        public SelectableItem(T value, string name)
        {
            Value = value;
            Name = name;
        }

        public T Value { get; }
        public string Name { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}