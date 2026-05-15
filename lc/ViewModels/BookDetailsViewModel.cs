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

    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private int _loadVersion;
    private bool _isDisposed;

    private Book? _book;
    private ImageSource? _coverImage;
    private bool _isLoading;
    private bool _isFavorite;
    private bool _isInLibrary;
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
        IUserLibraryService userLibraryService)
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

        BackCommand = new RelayCommand(_ => _navigationService.NavigateBack());
        ReloadCommand = new AsyncRelayCommand(_ => ReloadAsync(), _ => !IsLoading);

        StartReadingCommand = new AsyncRelayCommand(_ => StartReadingAsync(), _ => CanRead);
        OpenChapterCommand = new RelayCommand(OpenChapter, _ => CanRead);

        ToggleFavoriteCommand = new AsyncRelayCommand(_ => ToggleFavoriteAsync(), _ => CanToggleLibraryActions);
        ToggleLibraryCommand = new AsyncRelayCommand(_ => ToggleLibraryAsync(), _ => CanToggleLibraryActions);

        AddCommentCommand = new AsyncRelayCommand(_ => AddCommentAsync(), _ => CanComment);
        RateBookCommand = new AsyncRelayCommand(RateBookAsync, _ => CanRate);

        EditBookCommand = new RelayCommand(_ => EditBook(), _ => CanEditBook);
        ArchiveBookCommand = new AsyncRelayCommand(_ => ArchiveBookAsync(), _ => CanArchiveBook);
        DeleteBookCommand = new AsyncRelayCommand(_ => DeleteBookAsync(), _ => CanDeleteBook);

        _appState.PropertyChanged += OnAppStatePropertyChanged;

        _ = ReloadAsync();
    }

    public int BookId { get; }

    public ObservableCollection<string> Tags { get; } = [];
    public ObservableCollection<string> Categories { get; } = [];
    public ObservableCollection<Chapter> Chapters { get; } = [];
    public ObservableCollection<Comment> Comments { get; } = [];

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

    public ImageSource? CoverImage
    {
        get => _coverImage;
        private set => SetProperty(ref _coverImage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                RaiseCommandStates();
                OnPropertyChanged(nameof(CanRead));
                OnPropertyChanged(nameof(CanComment));
                OnPropertyChanged(nameof(CanToggleLibraryActions));
                OnPropertyChanged(nameof(CanRate));
                OnPropertyChanged(nameof(CanEditBook));
                OnPropertyChanged(nameof(CanArchiveBook));
                OnPropertyChanged(nameof(CanDeleteBook));
            }
        }
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        private set
        {
            if (SetProperty(ref _isFavorite, value))
                OnPropertyChanged(nameof(FavoriteButtonText));
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

    public bool IsAuthenticated => _appState.IsAuthenticated;
    public bool IsAdmin => _appState.IsAdmin;
    public bool IsWriter => _appState.IsWriter;

    public bool IsOwner =>
        _appState.CurrentUser?.UserId is not null &&
        Book is not null &&
        _appState.CurrentUser.UserId == Book.PublisherId;

    public bool CanRead =>
        Book is not null &&
        Book.BookStatus == BookStatus.Published &&
        Chapters.Count > 0 &&
        !IsLoading;

    public bool CanToggleLibraryActions =>
        IsAuthenticated &&
        Book is not null &&
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
        (IsAdmin || IsOwner) &&
        !IsLoading;

    public bool CanArchiveBook =>
        Book is not null &&
        (IsAdmin || IsOwner) &&
        Book.BookStatus != BookStatus.Archived &&
        !IsLoading;

    public bool CanDeleteBook =>
        Book is not null &&
        IsAdmin &&
        !IsLoading;

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
    public string ChaptersCountText => Chapters.Count.ToString();
    public string SymbolsCountText => Book is null ? "—" : FormatNumber(Book.SymbolsCount);

    public string FavoriteButtonText => IsFavorite ? "❤ В избранном" : "❤ В избранное";
    public string LibraryButtonText => IsInLibrary ? "Убрать из библиотеки" : "Добавить в список";
    public string ReadButtonText => CanRead ? "Читать" : "Чтение недоступно";

    public ICommand BackCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand StartReadingCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand ToggleLibraryCommand { get; }
    public ICommand AddCommentCommand { get; }
    public ICommand RateBookCommand { get; }
    public ICommand EditBookCommand { get; }
    public ICommand ArchiveBookCommand { get; }
    public ICommand DeleteBookCommand { get; }
    public ICommand OpenChapterCommand { get; }

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

            var bookTask = _bookService.GetBookByIdAsync(BookId);
            var chaptersTask = _chapterService.GetByBookIdAsync(BookId);
            var commentsTask = _commentService.GetByBookIdAsync(BookId);

            Task<bool>? inLibraryTask = null;
            Task<bool>? favoriteTask = null;

            if (IsAuthenticated)
            {
                inLibraryTask = _userLibraryService.IsBookInLibraryAsync(BookId);
                favoriteTask = _userLibraryService.IsBookFavoriteAsync(BookId);
            }

            var allTasks = new List<Task> { bookTask, chaptersTask, commentsTask };
            if (inLibraryTask is not null) allTasks.Add(inLibraryTask);
            if (favoriteTask is not null) allTasks.Add(favoriteTask);

            await Task.WhenAll(allTasks);

            if (version != Volatile.Read(ref _loadVersion))
                return;

            var book = await bookTask;
            if (book is null)
            {
                ClearState();
                ErrorMessage = "Книга не найдена.";
                return;
            }

            var chapters = (await chaptersTask)
                .OrderBy(x => x.ChapterNumber)
                .ToList();

            var comments = (await commentsTask)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            Book = book;
            CoverImage = LoadImageSource(book.CoverImagePath);

            Tags.Clear();
            foreach (var tag in book.Tags.OrderBy(x => x.Name))
            {
                if (!string.IsNullOrWhiteSpace(tag.Name))
                    Tags.Add(tag.Name);
            }

            Categories.Clear();
            foreach (var category in book.Categories.OrderBy(x => x.Name))
            {
                if (!string.IsNullOrWhiteSpace(category.Name))
                    Categories.Add(category.Name);
            }

            Chapters.Clear();
            foreach (var chapter in chapters)
                Chapters.Add(chapter);

            Comments.Clear();
            foreach (var comment in comments)
                Comments.Add(comment);

            IsInLibrary = inLibraryTask is not null && await inLibraryTask;
            IsFavorite = favoriteTask is not null && await favoriteTask;

            RaiseBookDependentProperties();
        }
        catch (Exception)
        {
            ClearState();
            ErrorMessage = "Не удалось загрузить данные книги.";
        }
        finally
        {
            IsLoading = false;
            _loadGate.Release();
            RaiseCommandStates();
        }
    }

    private void ClearState()
    {
        Book = null;
        CoverImage = null;
        Tags.Clear();
        Categories.Clear();
        Chapters.Clear();
        Comments.Clear();
        IsFavorite = false;
        IsInLibrary = false;

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
        OnPropertyChanged(nameof(FavoriteButtonText));
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
        OnPropertyChanged(nameof(FavoriteButtonText));
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

        if (ToggleFavoriteCommand is AsyncRelayCommand favorite)
            favorite.RaiseCanExecuteChanged();

        if (ToggleLibraryCommand is AsyncRelayCommand library)
            library.RaiseCanExecuteChanged();

        if (AddCommentCommand is AsyncRelayCommand comment)
            comment.RaiseCanExecuteChanged();

        if (RateBookCommand is AsyncRelayCommand rate)
            rate.RaiseCanExecuteChanged();

        if (EditBookCommand is RelayCommand edit)
            edit.RaiseCanExecuteChanged();

        if (ArchiveBookCommand is AsyncRelayCommand archive)
            archive.RaiseCanExecuteChanged();

        if (DeleteBookCommand is AsyncRelayCommand delete)
            delete.RaiseCanExecuteChanged();

        if (OpenChapterCommand is RelayCommand openChapter)
            openChapter.RaiseCanExecuteChanged();
    }

    private async Task StartReadingAsync()
    {
        if (!CanRead || Book is null)
            return;

        var firstChapter = Chapters.OrderBy(x => x.ChapterNumber).FirstOrDefault();
        if (firstChapter is null)
        {
            await _dialogService.ShowMessageAsync("Уведомление", "В этой книге пока нет глав.");
            return;
        }

        _navigationService.NavigateTo<ReaderViewModel>(Book.BookId, firstChapter.ChapterNumber);
    }

    private void OpenChapter(object? parameter)
    {
        if (!CanRead || Book is null)
            return;

        if (parameter is not Chapter chapter)
            return;

        _navigationService.NavigateTo<ReaderViewModel>(Book.BookId, chapter.ChapterNumber);
    }

    private async Task ToggleFavoriteAsync()
    {
        if (!CanToggleLibraryActions || Book is null)
            return;

        try
        {
            if (IsFavorite)
            {
                await _userLibraryService.RemoveFromFavoritesAsync(Book.BookId);
                IsFavorite = false;
            }
            else
            {
                await _userLibraryService.AddToFavoritesAsync(Book.BookId);
                IsFavorite = true;
            }
        }
        catch (Exception)
        {
            await _dialogService.ShowMessageAsync("Ошибка", "Не удалось изменить избранное.");
        }
    }

    private async Task ToggleLibraryAsync()
    {
        if (!CanToggleLibraryActions || Book is null)
            return;

        try
        {
            if (IsInLibrary)
            {
                await _userLibraryService.RemoveFromLibraryAsync(Book.BookId);
                IsInLibrary = false;
            }
            else
            {
                await _userLibraryService.AddToLibraryAsync(Book.BookId);
                IsInLibrary = true;
            }
        }
        catch (Exception)
        {
            await _dialogService.ShowMessageAsync("Ошибка", "Не удалось изменить библиотеку.");
        }
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
        catch (Exception)
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
        catch (Exception)
        {
            await _dialogService.ShowMessageAsync("Ошибка", "Не удалось сохранить оценку.");
        }
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

    private void EditBook()
    {
        if (!CanEditBook || Book is null)
            return;

        _navigationService.NavigateTo<EditBookViewModel>(Book.BookId);
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
        catch (Exception)
        {
            await _dialogService.ShowMessageAsync("Ошибка", "Не удалось архивировать книгу.");
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
        catch (Exception)
        {
            await _dialogService.ShowMessageAsync("Ошибка", "Не удалось удалить книгу.");
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
        if (e.PropertyName is not nameof(AppState.CurrentUser) and not nameof(AppState.SelectedBook))
            return;

        RaiseBookDependentProperties();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _appState.PropertyChanged -= OnAppStatePropertyChanged;
        _loadGate.Dispose();
    }
}