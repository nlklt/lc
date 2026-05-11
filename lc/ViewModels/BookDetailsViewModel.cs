using lc.Commands;
using lc.Infrastructure;
using lc.Models;
using lc.Models.Enums;
using lc.Services;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Documents;
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
            _chapterService = ServiceLocator.ChapterService;
            _commentService = ServiceLocator.CommentService;
            _dialogService = ServiceLocator.DialogService;
            _navigationService = ServiceLocator.NavigationService;
            _userLibraryService = ServiceLocator.UserLibraryService;
            _windowService = ServiceLocator.WindowService;

            BackCommand = new RelayCommand(_ => _navigationService.GoBack());
            StartReadingCommand = new RelayCommand(async _ => await StartReadingAsync(), _ => CanRead);
            OpenChapterCommand = new RelayCommand(async chapter => await OpenChapterAsync(chapter as Chapter), chapter => chapter is Chapter);
            ToggleFavoriteCommand = new RelayCommand(async _ => await ToggleFavoriteAsync(), _ => CanFavorite);
            ToggleLibraryCommand = new RelayCommand(async _ => await ToggleLibraryAsync(), _ => CanLibrary);
            AddCommentCommand = new RelayCommand(async _ => await AddCommentAsync(), _ => CanAddComment);
            EditBookCommand = new RelayCommand(_ => EditBook(), _ => CanManageBook);
            DeleteBookCommand = new RelayCommand(async _ => await DeleteBookAsync(), _ => CanManageBook);
            ReloadCommand = new RelayCommand(async _ => await ReloadAsync(), _ => BookId > 0);

            _ = InitializeAsync(bookId);
            UpdateStateFromApp();
        }

        public int BookId { get; private set; }

        public ObservableCollection<string> Tags { get; } = new();
        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<Chapter> Chapters { get; } = new();
        public ObservableCollection<Comment> Comments { get; } = new();

        public Book? Book
        {
            get => _book;
            private set
            {
                if (SetProperty(ref _book, value))
                {
                    OnPropertyChanged(string.Empty);
                    OnPropertyChanged(nameof(CanRead));
                    OnPropertyChanged(nameof(CanFavorite));
                    OnPropertyChanged(nameof(CanLibrary));
                    OnPropertyChanged(nameof(CanAddComment));
                    OnPropertyChanged(nameof(CanManageBook));
                    OnPropertyChanged(nameof(LibraryButtonText));
                    OnPropertyChanged(nameof(FavoriteButtonText));
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
            set
            {
                if (SetProperty(ref _isFavorite, value))
                    OnPropertyChanged(nameof(FavoriteButtonText));
            }
        }

        public bool IsInLibrary
        {
            get => _isInLibrary;
            set
            {
                if (SetProperty(ref _isInLibrary, value))
                    OnPropertyChanged(nameof(LibraryButtonText));
            }
        }

        public string NewCommentText
        {
            get => _newCommentText;
            set => SetProperty(ref _newCommentText, value);
        }

        public int SelectedRating
        {
            get => _selectedRating;
            set => SetProperty(ref _selectedRating, value);
        }

        public bool IsAuthenticated => _appState?.IsAuthenticated ?? false;
        public bool CanRead => Book is not null && IsAuthenticated && Chapters.Count > 0;
        public bool CanFavorite => Book is not null && IsAuthenticated;
        public bool CanLibrary => Book is not null && IsAuthenticated;
        public bool CanAddComment => Book is not null && IsAuthenticated && SelectedRating > 0;
        public bool CanManageBook => Book is not null && IsAuthenticated && IsCurrentUserOwnerOrAdmin();

        public string LibraryButtonText => IsInLibrary ? "Убрать из списка" : "Добавить в список";
        public string FavoriteButtonText => IsFavorite ? "Убрать из избранного" : "В избранное";

        public string Title => Book?.Title ?? "Без названия";
        public string Subtitle => BuildSubtitle();
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
        public ICommand OpenChapterCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand ToggleLibraryCommand { get; }
        public ICommand AddCommentCommand { get; }
        public ICommand EditBookCommand { get; }
        public ICommand DeleteBookCommand { get; }
        public ICommand ReloadCommand { get; }

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

                var book = await _bookService.GetBookByIdAsync(BookId);
                Book = book;

                LoadCollections(book);
                CoverImage = LoadImageSource(book?.CoverImagePath);

                Chapters.Clear();
                var chapters = await _chapterService.GetByBookIdAsync(BookId);
                foreach (var chapter in chapters)
                    Chapters.Add(chapter);

                Comments.Clear();
                var comments = await _commentService.GetByBookIdAsync(BookId);
                foreach (var comment in comments)
                    Comments.Add(comment);

                if (IsAuthenticated)
                {
                    IsInLibrary = await _userLibraryService.IsBookInLibraryAsync(BookId);
                    IsFavorite = await _userLibraryService.IsBookFavoriteAsync(BookId);
                }
                else
                {
                    IsInLibrary = false;
                    IsFavorite = false;
                }

                OnPropertyChanged(nameof(CanRead));
                OnPropertyChanged(nameof(CanAddComment));
                OnPropertyChanged(nameof(CanManageBook));
                OnPropertyChanged(nameof(ChaptersCountText));
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("Ошибка", $"Не удалось обновить данные: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
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
            OnPropertyChanged(nameof(CanAddComment));
            OnPropertyChanged(nameof(CanFavorite));
            OnPropertyChanged(nameof(CanLibrary));
            OnPropertyChanged(nameof(CanManageBook));
        }

        private async Task StartReadingAsync()
        {
            if (Book is null || Chapters.Count == 0) return;
            await _windowService.OpenReaderAsync(Book.BookId, Chapters[0].ChapterId);
        }

        private async Task OpenChapterAsync(Chapter? chapter)
        {
            if (Book is null || chapter is null) return;
            await _windowService.OpenReaderAsync(Book.BookId, chapter.ChapterId);
        }

        private async Task ToggleFavoriteAsync()
        {
            if (Book is null) return;

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

        private async Task ToggleLibraryAsync()
        {
            if (Book is null) return;

            try
            {
                if (IsInLibrary)
                {
                    await _userLibraryService.RemoveBookFromListAsync(
                        _appState.CurrentUser!.UserId, 
                        0, 
                        Book.BookId);
                    IsInLibrary = false;
                }
                else
                {
                    await _userLibraryService.AddBookToListAsync(
                        _appState.CurrentUser!.UserId,
                        0,
                        Book.BookId);
                    IsInLibrary = true;
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
            }
        }

        private async Task AddCommentAsync()
        {
            if (Book is null || !IsAuthenticated) return;

            var text = (NewCommentText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                await _dialogService.ShowMessageAsync("Проверка", "Введите комментарий.");
                return;
            }

            if (SelectedRating < 1 || SelectedRating > 10)
            {
                await _dialogService.ShowMessageAsync("Проверка", "Оценка должна быть от 1 до 10.");
                return;
            }

            try
            {
                var userId = _appState.CurrentUser!.UserId;
                await _commentService.AddAsync(Book.BookId, userId, text, SelectedRating);

                NewCommentText = string.Empty;
                SelectedRating = 10;

                await ReloadAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
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

            var confirmed = await _dialogService.ShowConfirmAsync(
                "Удаление книги",
                $"Удалить книгу «{Book.Title}»? Это действие нельзя отменить.");

            if (!confirmed) return;

            try
            {
                await _bookService.DeleteBookAsync(Book.BookId);
                await _dialogService.ShowMessageAsync("Готово", "Книга удалена.");
                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
            }
        }

        private bool IsCurrentUserOwnerOrAdmin()
        {
            if (Book is null || _appState.CurrentUser is null) return false;

            return _appState.CurrentUser.Role == UserRole.Admin
                   || (Book.Publisher != null && Book.Publisher.UserId == _appState.CurrentUser.UserId);
        }

        private string BuildSubtitle()
        {
            if (Book is null) return string.Empty;

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Book.AuthorName))
                parts.Add(Book.AuthorName);

            if (Book.CreatedAt != default)
                parts.Add(Book.CreatedAt.ToString("yyyy", CultureInfo.CurrentCulture));

            if (Book.Rating > 0)
                parts.Add($"{Book.Rating:0.0} ★");

            return string.Join(" • ", parts);
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