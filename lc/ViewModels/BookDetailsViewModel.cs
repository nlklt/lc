using lc.Commands;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using lc.Services;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace lc.ViewModels
{
    public class BookDetailsViewModel : ViewModelBase
    {
        private readonly AppState _appState;
        private readonly IBookService _bookService;
        private readonly IChapterService _chapterService;
        private readonly ICommentService _commentService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;
        private readonly IUserLibraryService _userLibraryService;
        private readonly IWindowService _windowService;

        private readonly IChapterRepository _chapterRepository;
        private readonly ICommentRepository _commentRepository;

        private Book? _book;
        private ImageSource? _coverImage;
        private bool _isLoading;
        private bool _isFavorite;
        private bool _isInLibrary;
        private string _newCommentText = string.Empty;
        private int _selectedRating = 10;

        public BookDetailsViewModel(int bookId)
        {
            _appState = ServiceLocator.AppState;
            _bookService = ServiceLocator.BookService;
            _dialogService = ServiceLocator.DialogService;
            _navigationService = ServiceLocator.NavigationService;
            _userLibraryService = ServiceLocator.UserLibraryService;
            _windowService = ServiceLocator.WindowService;
            _chapterRepository = ServiceLocator.ChapterRepository;
            _commentRepository = ServiceLocator.CommentRepository;

            BackCommand = new RelayCommand(_ => _navigationService.GoBack());
            StartReadingCommand = new RelayCommand(_ => StartReading(), _ => CanRead);
            OpenChapterCommand = new RelayCommand(OpenChapter, _ => CanRead);
            ToggleFavoriteCommand = new AsyncRelayCommand(async _ => await ToggleFavoriteAsync());

            ToggleLibraryCommand = new AsyncRelayCommand(async _ => await AddToLibraryAsync(), _ => CanAddToLibrary);

            AddCommentCommand = new AsyncRelayCommand(async _ => await AddCommentAsync(), _ => CanComment);
            RateBookCommand = new AsyncRelayCommand(async rating => await RateBookAsync(rating), _ => IsAuthenticated);
            EditBookCommand = new RelayCommand(_ => EditBook(), _ => CanEditOrDelete);
            DeleteBookCommand = new AsyncRelayCommand(async _ => await DeleteBookAsync(), _ => CanEditOrDelete);

            _ = InitializeAsync(bookId);
            UpdateStateFromApp();
        }

        public int BookId { get; private set; }

        public ObservableCollection<string> Tags { get; } = new();
        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<Chapter> Chapters { get; } = new();
        public ObservableCollection<Comment> Comments { get; } = new();
        public ObservableCollection<int> Ratings { get; } = new() { 0, 1, 2, 3, 4, 5 };

        public string NewCommentText
        {
            get => _newCommentText;
            set
            {
                if (SetProperty(ref _newCommentText, value))
                    OnPropertyChanged(nameof(CanComment));
            }
        }

        public Book? Book
        {
            get => _book;
            private set
            {
                if (SetProperty(ref _book, value))
                {
                    OnPropertyChanged(string.Empty);
                }
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
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        public bool IsInLibrary
        {
            get => _isInLibrary;
            set => SetProperty(ref _isInLibrary, value);
        }

        public int SelectedRating
        {
            get => _selectedRating;
            set
            {
                var clamped = Math.Clamp(value, 0, 5);
                if (SetProperty(ref _selectedRating, clamped))
                    OnPropertyChanged(nameof(CanComment));
            }
        }

        public bool IsAuthenticated => _appState?.IsAuthenticated ?? false;
        public bool CanRead => Book is not null && IsAuthenticated;
        public bool CanFavorite => Book is not null && IsAuthenticated;
        public bool CanAddToLibrary => Book is not null && IsAuthenticated && !_isInLibrary;
        public bool CanComment => IsAuthenticated && !string.IsNullOrWhiteSpace(NewCommentText);

        public bool CanEditOrDelete => IsAuthenticated && Book is not null &&
            (_appState.CurrentUser?.UserId == Book.PublisherId || _appState.CurrentUser?.Role == UserRole.Admin);

        public string Title => Book?.Title ?? "Без названия";
        public string AuthorName => $"Автор: {Book?.AuthorName ?? "Не указан"}";
        public string PublisherName => $"Издатель: {Book?.Publisher?.UserName ?? "Не указан"}";
        public string Description => string.IsNullOrWhiteSpace(Book?.Description) ? "Описание отсутствует." : Book.Description;

        public string RatingText => Book is null ? "—" : $"{Book.Rating:0.0} ★";
        public string ViewsText => Book is null ? "—" : $"{FormatNumber(Book.Views)} просмотров";
        public string AgeRatingText => Book is null ? "—" : $"{Book.AgeRating}+";
        public string WritingStatusText => Book is null ? "—" : GetWritingStatusText(Book.WritingStatus);
        public string LanguageText => Book is null ? "—" : GetLanguageText(Book.Language);
        public string CreatedAtText => Book is null ? "—" : Book.CreatedAt.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture);
        public string UpdatedAtText => Book is null ? "—" : Book.UpdatedAt.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture);
        public string ChaptersCountText => Book is null ? "—" : Chapters.Count.ToString(CultureInfo.CurrentCulture);
        public string SymbolsCountText => Book is null ? "—" : FormatNumber(Book.SymbolsCount);

        public ICommand BackCommand { get; }
        public ICommand StartReadingCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand ToggleLibraryCommand { get; }
        public ICommand ReloadCommand { get; }

        public ICommand OpenChapterCommand { get; }
        public ICommand AddCommentCommand { get; }
        public ICommand RateBookCommand { get; }
        public ICommand EditBookCommand { get; }
        public ICommand DeleteBookCommand { get; }

        public async Task InitializeAsync(int bookId)
        {
            BookId = bookId;
            await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            if (BookId <= 0) return;

            try
            {
                IsLoading = true;

                if (_bookService == null) throw new Exception("BookService не инициализирован");

                var book = await _bookService.GetBookByIdAsync(BookId);
                Book = book;

                LoadCollections(book);
                CoverImage = LoadImageSource(book?.CoverImagePath);

                if (_chapterRepository == null) throw new Exception("ChapterRepository не инициализирован");
                var chapters = await _chapterRepository.GetByBookIdAsync(BookId);

                Chapters.Clear();
                foreach (var chapter in (chapters ?? Enumerable.Empty<Chapter>()).OrderBy(c => c.ChapterNumber))
                    Chapters.Add(chapter);

                if (_commentRepository == null) throw new Exception("CommentRepository не инициализирован");

                var comments = await _commentRepository.GetByBookIdAsync(BookId);

                Comments.Clear();
                foreach (var comment in (comments ?? Enumerable.Empty<Comment>()).OrderByDescending(c => c.CreatedAt))
                    Comments.Add(comment);

                if (IsAuthenticated)
                {
                    if (_userLibraryService == null) throw new Exception("UserLibraryService не инициализирован");
                    IsInLibrary = await _userLibraryService.IsBookInLibraryAsync(BookId);
                    IsFavorite = await _userLibraryService.IsBookFavoriteAsync(BookId);
                }
            }
            catch (Exception ex)
            {
                if (_dialogService != null)
                    await _dialogService.ShowMessageAsync("Ошибка", $"Не удалось обновить данные: {ex.Message}");
                else
                    System.Diagnostics.Debug.WriteLine($"Критическая ошибка: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                UpdateStateFromApp();
            }
        }

        private void LoadCollections(Book? book)
        {
            Tags.Clear();
            Categories.Clear();

            if (book == null) return;

            if (book.Tags != null)
            {
                foreach (var tag in book.Tags)
                    if (!string.IsNullOrWhiteSpace(tag?.Name))
                        Tags.Add(tag.Name);
            }

            if (book.Categories != null)
            {
                foreach (var category in book.Categories)
                    if (!string.IsNullOrWhiteSpace(category?.Name))
                        Categories.Add(category.Name);
            }
        }

        private void UpdateStateFromApp()
        {
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(CanRead));
            OnPropertyChanged(nameof(CanComment));
            OnPropertyChanged(nameof(CanFavorite));
            OnPropertyChanged(nameof(CanAddToLibrary));
            OnPropertyChanged(nameof(CanEditOrDelete));
        }

        private void StartReading()
        {
            var firstChapter = Chapters.FirstOrDefault();
            if (firstChapter != null)
            {
                OpenChapter(firstChapter);
            }
            else
            {
                _dialogService.ShowMessageAsync("Уведомление", "В этой книге пока нет глав.");
            }
        }

        private void OpenChapter(object? parameter)
        {
            if (parameter is Chapter chapter && Book != null)
            {
                var readerWindow = new lc.Views.Windows.ReaderWindow(Book.BookId, chapter.ChapterId);
                readerWindow.Show();
            }
        }

        private async Task ToggleFavoriteAsync()
        {
            if (Book is null || !IsAuthenticated) return;

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
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
            }
        }

        private async Task AddToLibraryAsync()
        {
            if (Book is null || !IsAuthenticated) return;

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
                UpdateStateFromApp();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
            }
        }

        private async Task AddCommentAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCommentText)) return;

            try
            {
                if (_appState?.CurrentUser == null)
                {
                    await _dialogService.ShowMessageAsync("Ошибка", "Необходимо войти в систему.");
                    return;
                }

                if (_commentRepository == null)
                {
                    await _dialogService.ShowMessageAsync("Ошибка", "Репозиторий не инициализирован.");
                    return;
                }

                var comment = new Comment
                {
                    UserId = _appState.CurrentUser.UserId,
                    BookId = BookId,
                    Text = NewCommentText,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _commentRepository.CreateAsync(comment);

                Comments.Insert(0, comment);
                NewCommentText = string.Empty;
            }
            catch (Exception ex)
            {
                if (_dialogService != null)
                    await _dialogService.ShowMessageAsync("Ошибка", $"Ошибка: {ex.Message}");
            }
        }

        private async Task RateBookAsync(object? ratingParameter)
        {
            if (ratingParameter is string ratingStr && int.TryParse(ratingStr, out int rating))
            {
                try
                {
                    if (_appState?.CurrentUser == null)
                    {
                        await _dialogService.ShowMessageAsync("Ошибка", "Для оценки книги необходимо войти в аккаунт.");
                        return;
                    }

                    if (_bookService == null)
                    {
                        await _dialogService.ShowMessageAsync("Ошибка", "Сервис книг недоступен.");
                        return;
                    }

                    await _bookService.AddRatingAsync(BookId, _appState.CurrentUser.UserId, rating);

                    await ReloadAsync();
                    await _dialogService.ShowMessageAsync("Успех", "Оценка сохранена!");
                }
                catch (Exception ex)
                {
                    if (_dialogService != null)
                        await _dialogService.ShowMessageAsync("Ошибка", $"Произошла ошибка при сохранении: {ex.Message}");
                }
            }
        }

        private void EditBook()
        {
            if (Book is null) return;
            _navigationService.Navigate(new EditBookViewModel(Book.BookId));
        }

        private async Task DeleteBookAsync()
        {
            if (Book is null) return;

            bool confirm = await _dialogService.ShowConfirmAsync("Внимание", "Вы уверены, что хотите безвозвратно удалить эту книгу?");
            if (confirm)
            {
                try
                {
                    await _bookService.DeleteBookAsync(BookId);
                    _navigationService.GoBack();
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
                }
            }
        }

        private static string FormatNumber(long value) => value.ToString("N0", CultureInfo.CurrentCulture);

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
            if (string.IsNullOrWhiteSpace(path)) return null;

            try
            {
                var fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath)) return null;

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
    }
}