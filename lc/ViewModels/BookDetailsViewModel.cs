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
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace lc.ViewModels;

public sealed class BookDetailsViewModel : ViewModelBase, IDisposable
{
    private readonly AppState _appState;
    private readonly IBookService _bookService;
    private readonly IChapterService _chapterService;
    private readonly ICommentService _commentService;
    private readonly IBookStatsService _bookStatsService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IUserLibraryService _userLibraryService;
    private readonly IWindowService _windowService;
    private readonly IReadingProgressService _readingProgressService;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private int _loadVersion;
    private bool _isDisposed;

    private Book? _book;
    private ImageSource? _coverPath;
    private bool _isLoading;
    private bool _isInLibrary;
    private int _publishedChaptersCount;
    private string _newCommentText = string.Empty;
    private string _errorMessage = string.Empty;

    public BookDetailsViewModel(
    int bookId,
    AppState appState,
    IBookService bookService,
    IChapterService chapterService,
    ICommentService commentService,
    IBookStatsService bookStatsService,
    IDialogService dialogService,
    INavigationService navigationService,
    IUserLibraryService userLibraryService,
    IWindowService windowService,
    IReadingProgressService readingProgressService)
    {
        if (bookId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bookId));

        BookId = bookId;
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
        _chapterService = chapterService ?? throw new ArgumentNullException(nameof(chapterService));
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
        _bookStatsService = bookStatsService ?? throw new ArgumentNullException(nameof(bookStatsService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _userLibraryService = userLibraryService ?? throw new ArgumentNullException(nameof(userLibraryService));
        _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        _readingProgressService = readingProgressService ?? throw new ArgumentNullException(nameof(readingProgressService));

        BackCommand = new RelayCommand(_ => _navigationService.NavigateBack());
        ReloadCommand = new AsyncRelayCommand(_ => ReloadAsync(), _ => !IsLoading);

        ToggleLibraryCommand = new AsyncRelayCommand(_ => ToggleLibraryAsync(), _ => CanToggleLibraryActions);

        AddCommentCommand = new AsyncRelayCommand(_ => AddCommentAsync(), _ => CanComment);
        RateBookCommand = new AsyncRelayCommand(RateBookAsync, _ => CanRate);

        EditBookCommand = new RelayCommand(_ => EditBook(), _ => CanEditBook);
        ToggleArchiveCommand = new AsyncRelayCommand(_ => ToggleArchiveAsync(), _ => CanToggleArchiveBook);
        DeleteBookCommand = new AsyncRelayCommand(_ => DeleteBookAsync(), _ => CanDeleteBook);

        StartReadingCommand = new AsyncRelayCommand(StartReadingAsync, () => !IsLoading && CanRead);
        OpenChapterCommand = new AsyncRelayCommand(OpenChapterAsync, _ => !IsLoading);

        AddChapterCommand = new RelayCommand(_ => AddChapter(), _ => CanAddChapter);
        EditChapterCommand = new RelayCommand(EditChapter, _ => !IsLoading);
        DeleteChapterCommand = new AsyncRelayCommand(DeleteChapterAsync, _ => !IsLoading);

        _appState.PropertyChanged += OnAppStatePropertyChanged;
        _ = ReloadAsync();
    }

    public int BookId { get; }

    public ObservableCollection<string> Tags { get; } = [];
    public ObservableCollection<string> Categories { get; } = [];
    public ObservableCollection<Comment> Comments { get; } = [];
    public ObservableCollection<ChapterItem> Chapters { get; } = [];

    public string NewCommentText
    {
        get => _newCommentText;
        set
        {
            var normalized = value ?? string.Empty;
            if (SetProperty(ref _newCommentText, normalized))
                RaiseCommandStates();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public Book? Book
    {
        get => _book;
        private set
        {
            if (SetProperty(ref _book, value))
                RaiseBookDependentProperties();
        }
    }

    public ImageSource? CoverPath
    {
        get => _coverPath;
        private set => SetProperty(ref _coverPath, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                RaiseCommandStates();
                RaiseBookDependentProperties();
            }
        }
    }

    public bool IsInLibrary
    {
        get => _isInLibrary;
        private set
        {
            if (SetProperty(ref _isInLibrary, value))
                OnPropertyChanged(nameof(LibraryButtonText));
        }
    }

    public int PublishedChaptersCount
    {
        get => _publishedChaptersCount;
        private set
        {
            if (SetProperty(ref _publishedChaptersCount, value))
            {
                OnPropertyChanged(nameof(ChaptersCountText));
                OnPropertyChanged(nameof(CanRead));
            }
        }
    }

    public bool IsAuthenticated => _appState.IsAuthenticated;
    public bool IsAdmin => _appState.IsAdmin;
    public bool IsWriter => _appState.IsWriter;

    public bool IsOwner =>
        _appState.CurrentUser is not null &&
        Book is not null &&
        _appState.CurrentUser.UserId == Book.PublisherId;

    public bool CanRead =>
        Book is not null &&
        Book.BookStatus == BookStatus.Published &&
        PublishedChaptersCount > 0 &&
        !IsLoading;

    public bool CanToggleLibraryActions =>
        IsAuthenticated &&
        Book is not null &&
        Book.BookStatus == BookStatus.Published &&
        !IsLoading;

    public bool CanRate =>
        IsAuthenticated &&
        Book is not null &&
        Book.BookStatus == BookStatus.Published &&
        !IsLoading;

    public bool CanComment =>
        IsAuthenticated &&
        Book is not null &&
        Book.BookStatus == BookStatus.Published &&
        !string.IsNullOrWhiteSpace(NewCommentText) &&
        !IsLoading;

    public bool CanEditBook =>
        Book is not null &&
        !IsLoading &&
        (IsOwner || IsAdmin) &&
        Book.BookStatus != BookStatus.Archived;

    public bool CanToggleArchiveBook =>
        Book is not null &&
        !IsLoading &&
        IsAdmin &&
        (Book.BookStatus == BookStatus.Published || Book.BookStatus == BookStatus.Archived);

    public bool CanAddChapter =>
        Book is not null &&
        !IsLoading &&
        Book.BookStatus != BookStatus.Archived &&
        (IsAdmin || IsOwner);

    public bool CanArchiveBook =>
        Book is not null &&
        !IsLoading &&
        IsAdmin &&
        Book.BookStatus != BookStatus.Archived;

    public bool CanDeleteBook =>
        Book is not null &&
        !IsLoading &&
        (IsOwner || IsAdmin);

    public string Title => Book?.Title ?? "Без названия";
    public string AuthorName => $"Автор: {Book?.AuthorName ?? "Не указан"}";
    public string PublisherName => $"Издатель: {Book?.Publisher?.UserName ?? "Не указан"}";
    public string Description => string.IsNullOrWhiteSpace(Book?.Description) ? "Описание отсутствует." : Book.Description;

    public string RatingText => Book is null ? "—" : $"{Book.Rating:0.0} ★";
    public string ViewsText => Book is null ? "—" : $"{FormatNumber(Book.Views)} просмотров";
    public string AgeRatingText => Book is null ? "—" : $"{Book.AgeRating}+";
    public string WritingStatusText => Book is null ? "—" : GetWritingStatusText(Book.WritingStatus);
    public string LanguageText => Book is null ? "—" : GetLanguageText(Book.Language);
    public string CreatedAtText => Book is null ? "—" : Book.CreatedAt.ToString("dd.MM.yyyy");
    public string UpdatedAtText => Book is null ? "—" : Book.UpdatedAt.ToString("dd.MM.yyyy");
    public string ChaptersCountText => PublishedChaptersCount.ToString();
    public string SymbolsCountText => Book is null ? "—" : FormatNumber(Book.SymbolsCount);

    public string LibraryButtonText => IsInLibrary ? "Убрать из списка" : "Добавить в список";
    public string ReadButtonText => CanRead ? "Читать" : "Чтение недоступно";
    public string ArchiveButtonText => Book?.BookStatus == BookStatus.Archived ? "Из архива" : "В архив";
    public string BookStatusOrLangField => CanEditBook ? "Состояние" : "Язык";
    public string BookStatusOrLangText => CanEditBook
        ? Book?.BookStatus switch
        {
            BookStatus.Draft => "Черновик",
            BookStatus.Archived => "В архиве",
            BookStatus.Published => "Опубликована",
            _ => "Черновик"
        }
        : (Book?.Language ?? Language.Русский).ToString();

    public ICommand BackCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand StartReadingCommand { get; }
    public ICommand ToggleLibraryCommand { get; }
    public ICommand AddCommentCommand { get; }
    public ICommand RateBookCommand { get; }

    public ICommand EditBookCommand { get; }
    public ICommand ArchiveBookCommand { get; }
    public ICommand ToggleArchiveCommand { get; }
    public ICommand DeleteBookCommand { get; }

    public ICommand OpenChapterCommand { get; }
    public ICommand AddChapterCommand { get; }
    public ICommand EditChapterCommand { get; }
    public ICommand DeleteChapterCommand { get; }

    public async Task ReloadAsync()
    {
        if (_isDisposed || BookId <= 0)
            return;

        var version = Interlocked.Increment(ref _loadVersion);

        await _loadGate.WaitAsync();
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var book = await _bookService.GetBookByIdAsync(BookId);
            if (book is null)
            {
                ClearState();
                ErrorMessage = "Книга не найдена.";
                return;
            }

            if (version != Volatile.Read(ref _loadVersion))
                return;

            Book = book;
            CoverPath = LoadImageSource(book.CoverImagePath);

            Tags.Clear();
            foreach (var tag in book.Tags ?? [])
            {
                if (!string.IsNullOrWhiteSpace(tag.Name))
                    Tags.Add(tag.Name);
            }

            Categories.Clear();
            foreach (var category in book.Categories ?? [])
            {
                if (!string.IsNullOrWhiteSpace(category.Name))
                    Categories.Add(category.Name);
            }

            RaiseBookDependentProperties();

            var canSeeDrafts = _appState.IsAdmin || (_appState.CurrentUser?.UserId == book.PublisherId);

            try
            {
                var chapters = await _chapterService.GetByBookIdAsync(BookId, includeDrafts: canSeeDrafts);
                var orderedChapters = chapters.OrderBy(x => x.ChapterNumber).ToList();

                Chapters.Clear();
                foreach (var chapter in orderedChapters)
                {
                    Chapters.Add(new ChapterItem(
                        chapter,
                        canOpen: chapter.Status == ChapterStatus.Published && book.BookStatus == BookStatus.Published,
                        canEdit: CanEditChapter(chapter, book),
                        canDelete: CanDeleteChapter(chapter, book)));
                }

                PublishedChaptersCount = orderedChapters.Count(x => x.Status == ChapterStatus.Published);
            }
            catch
            {
                Chapters.Clear();
                PublishedChaptersCount = 0;
            }

            try
            {
                var comments = await _commentService.GetByBookIdAsync(BookId);
                var orderedComments = comments.OrderByDescending(x => x.CreatedAt).ToList();

                Comments.Clear();
                foreach (var comment in orderedComments)
                    Comments.Add(comment);
            }
            catch
            {
                Comments.Clear();
            }

            try
            {
                IsInLibrary = IsAuthenticated && book.BookStatus == BookStatus.Published
                    ? await _userLibraryService.IsBookInLibraryAsync(BookId)
                    : false;
            }
            catch
            {
                IsInLibrary = false;
            }

            RaiseBookDependentProperties();
        }
        catch
        {
            if (version == Volatile.Read(ref _loadVersion))
            {
                ClearState();
                ErrorMessage = "Не удалось загрузить данные книги.";
            }
        }
        finally
        {
            if (version == Volatile.Read(ref _loadVersion))
                IsLoading = false;

            _loadGate.Release();
            RaiseCommandStates();
        }
    }

    private void ClearState()
    {
        Book = null;
        CoverPath = null;
        Tags.Clear();
        Categories.Clear();
        Chapters.Clear();
        Comments.Clear();
        IsInLibrary = false;
        PublishedChaptersCount = 0;

        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(AuthorName));
        OnPropertyChanged(nameof(PublisherName));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(RatingText));
        OnPropertyChanged(nameof(ViewsText));
        OnPropertyChanged(nameof(AgeRatingText));
        OnPropertyChanged(nameof(WritingStatusText));
        OnPropertyChanged(nameof(LanguageText));
        OnPropertyChanged(nameof(CreatedAtText));
        OnPropertyChanged(nameof(UpdatedAtText));
        OnPropertyChanged(nameof(ChaptersCountText));
        OnPropertyChanged(nameof(SymbolsCountText));
        OnPropertyChanged(nameof(LibraryButtonText));
        OnPropertyChanged(nameof(ReadButtonText));
    }

    private void RaiseBookDependentProperties()
    {
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(IsWriter));
        OnPropertyChanged(nameof(IsOwner));

        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanToggleLibraryActions));
        OnPropertyChanged(nameof(CanRate));
        OnPropertyChanged(nameof(CanComment));
        OnPropertyChanged(nameof(CanEditBook));
        OnPropertyChanged(nameof(CanAddChapter));
        OnPropertyChanged(nameof(CanArchiveBook));
        OnPropertyChanged(nameof(CanDeleteBook));

        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(AuthorName));
        OnPropertyChanged(nameof(PublisherName));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(RatingText));
        OnPropertyChanged(nameof(ViewsText));
        OnPropertyChanged(nameof(AgeRatingText));
        OnPropertyChanged(nameof(WritingStatusText));
        OnPropertyChanged(nameof(LanguageText));
        OnPropertyChanged(nameof(CreatedAtText));
        OnPropertyChanged(nameof(UpdatedAtText));
        OnPropertyChanged(nameof(ChaptersCountText));
        OnPropertyChanged(nameof(SymbolsCountText));
        OnPropertyChanged(nameof(LibraryButtonText));
        OnPropertyChanged(nameof(ReadButtonText));

        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        if (ReloadCommand is AsyncRelayCommand reload)
            reload.RaiseCanExecuteChanged();

        if (StartReadingCommand is AsyncRelayCommand read)
            read.RaiseCanExecuteChanged();

        if (ToggleLibraryCommand is AsyncRelayCommand library)
            library.RaiseCanExecuteChanged();

        if (AddCommentCommand is AsyncRelayCommand comment)
            comment.RaiseCanExecuteChanged();

        if (RateBookCommand is AsyncRelayCommand rate)
            rate.RaiseCanExecuteChanged();

        if (EditBookCommand is RelayCommand edit)
            edit.RaiseCanExecuteChanged();

        if (ToggleArchiveCommand is AsyncRelayCommand archive)
            archive.RaiseCanExecuteChanged();

        if (DeleteBookCommand is AsyncRelayCommand delete)
            delete.RaiseCanExecuteChanged();

        if (OpenChapterCommand is RelayCommand openChapter)
            openChapter.RaiseCanExecuteChanged();

        if (AddChapterCommand is RelayCommand addChapter)
            addChapter.RaiseCanExecuteChanged();

        if (EditChapterCommand is RelayCommand editChapter)
            editChapter.RaiseCanExecuteChanged();

        if (DeleteChapterCommand is AsyncRelayCommand deleteChapter)
            deleteChapter.RaiseCanExecuteChanged();
    }

    private async Task StartReadingAsync()
    {
        if (!CanRead || Book is null)
            return;

        await _windowService.OpenReaderAsync(Book.BookId);
    }

    private async Task OpenChapterAsync(object? parameter)
    {
        if (parameter is not ChapterItem item ||
            !item.CanOpen ||
            Book is null)
        {
            return;
        }

        await _windowService.OpenReaderAsync(
            Book.BookId,
            item.Chapter.ChapterNumber);
    }

    private void AddChapter()
    {
        if (!CanAddChapter || Book is null)
            return;

        _navigationService.NavigateTo<EditChapterViewModel>(Book.BookId);
    }

    private void EditChapter(object? parameter)
    {
        if (Book is null || parameter is not ChapterItem item || !item.CanEdit)
            return;

        _navigationService.NavigateTo<EditChapterViewModel>(Book.BookId, item.Chapter.ChapterId);
    }

    private async Task DeleteChapterAsync(object? parameter)
    {
        if (Book is null || parameter is not ChapterItem item || !item.CanDelete)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Удалить главу",
            $"Удалить главу «{item.Chapter.Title}»?");

        if (!confirmed)
            return;

        try
        {
            await _chapterService.DeleteAsync(item.Chapter.ChapterId);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
        }
    }

    private async Task ToggleLibraryAsync()
    {
        //if (!CanToggleLibraryActions || Book is null)
        //    return;

        //try
        //{
        //    if (IsInLibrary)
        //    {
        //        await _userLibraryService.RemoveFromLibraryAsync(Book.BookId);
        //        IsInLibrary = false;
        //    }
        //    else
        //    {
        //        await _userLibraryService.AddToLibraryAsync(Book.BookId);
        //        IsInLibrary = true;
        //    }
        //}
        //catch
        //{
        //    await _dialogService.ShowMessageAsync("Ошибка", "Не удалось изменить список.");
        //}
    }

    private async Task AddCommentAsync()
    {
        if (!CanComment || Book is null)
            return;

        var text = NewCommentText.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            await _commentService.AddAsync(Book.BookId, text);
            NewCommentText = string.Empty;
            await ReloadAsync();
        }
        catch
        {
            await _dialogService.ShowMessageAsync("Ошибка", "Не удалось добавить комментарий.");
        }
    }

    private async Task RateBookAsync(object? parameter)
    {
        if (!CanRate || Book is null)
            return;

        if (!TryParseRating(parameter, out var rating))
        {
            await _dialogService.ShowMessageAsync("Ошибка", "Некорректная оценка.");
            return;
        }

        try
        {
            await _bookStatsService.SetRatingAsync(Book.BookId, rating);
            await ReloadAsync();
        }
        catch
        {
            await _dialogService.ShowMessageAsync("Ошибка", "Не удалось сохранить оценку.");
        }
    }

    private void EditBook()
    {
        if (!CanEditBook || Book is null)
            return;

        _navigationService.NavigateTo<EditBookViewModel>(Book.BookId);
    }

    private async Task ToggleArchiveAsync()
    {
        if (!CanToggleArchiveBook || Book is null)
            return;

        var confirmed = Book.BookStatus == BookStatus.Archived
            ? await _dialogService.ShowConfirmAsync("Восстановить книгу", $"Вернуть книгу «{Book.Title}» из архива?")
            : await _dialogService.ShowConfirmAsync("Архивировать книгу", $"Перевести книгу «{Book.Title}» в архив?");

        if (!confirmed)
            return;

        try
        {
            if (Book.BookStatus == BookStatus.Archived)
                await _bookService.RestoreBookAsync(Book.BookId);
            else
                await _bookService.ArchiveBookAsync(Book.BookId);

            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
        }
    }

    private async Task ArchiveBookAsync()
    {
        if (!CanArchiveBook || Book is null)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Архивировать книгу",
            $"Перевести книгу «{Book.Title}» в архив?");

        if (!confirmed)
            return;

        try
        {
            await _bookService.ArchiveBookAsync(Book.BookId);
            _navigationService.NavigateTo<CatalogViewModel>();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
        }
    }

    private async Task DeleteBookAsync()
    {
        if (!CanDeleteBook || Book is null)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Удалить книгу",
            $"Удалить книгу «{Book.Title}» полностью?");

        if (!confirmed)
            return;

        try
        {
            await _bookService.DeleteBookAsync(Book.BookId);
            _navigationService.NavigateTo<CatalogViewModel>();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
        }
    }

    private bool CanEditChapter(Chapter chapter, Book book)
    {
        if (_appState.CurrentUser is null)
            return false;

        if (book.BookStatus == BookStatus.Archived)
            return IsAdmin;

        return chapter.Status == ChapterStatus.Draft && (IsOwner || IsAdmin);
    }

    private bool CanDeleteChapter(Chapter chapter, Book book)
    {
        if (_appState.CurrentUser is null)
            return false;

        if (book.BookStatus == BookStatus.Archived)
            return IsAdmin;

        return chapter.Status == ChapterStatus.Draft && (IsOwner || IsAdmin);
    }

    private static bool TryParseRating(object? parameter, out byte rating)
    {
        rating = 0;

        switch (parameter)
        {
            case byte b when b is >= 1 and <= 5:
                rating = b;
                return true;
            case int i when i is >= 1 and <= 5:
                rating = (byte)i;
                return true;
            case string s when byte.TryParse(s, out var parsed) && parsed is >= 1 and <= 5:
                rating = parsed;
                return true;
            default:
                return false;
        }
    }

    private static string FormatNumber(long value) => value.ToString("N0");

    private static string GetWritingStatusText(WritingStatus status) => status switch
    {
        WritingStatus.Онгоинг => "Онгоинг",
        WritingStatus.Анонс => "Анонс",
        WritingStatus.Отложена => "Отложена",
        WritingStatus.Завершена => "Завершена",
        WritingStatus.Брошена => "Брошена",
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

    private static ImageSource? LoadImageSource(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var fullPath = Path.IsPathRooted(path)
                ? path
                : Path.Combine(AppContext.BaseDirectory, path);

            if (!File.Exists(fullPath))
                return null;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(fullPath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private void OnAppStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppState.CurrentUser) or nameof(AppState.IsAdmin) or nameof(AppState.IsWriter))
            _ = ReloadAsync();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _appState.PropertyChanged -= OnAppStatePropertyChanged;
        _loadGate.Dispose();
    }

    public sealed class ChapterItem
    {
        public ChapterItem(Chapter chapter, bool canOpen, bool canEdit, bool canDelete)
        {
            Chapter = chapter ?? throw new ArgumentNullException(nameof(chapter));
            CanOpen = canOpen;
            CanEdit = canEdit;
            CanDelete = canDelete;
        }

        public Chapter Chapter { get; }
        public bool CanOpen { get; }
        public bool CanEdit { get; }
        public bool CanDelete { get; }
    }
}