using lc.Commands;
using lc.Infrastructure;
using lc.Models;
using lc.Models.Enums;
using lc.Services;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System;

namespace lc.ViewModels
{
    public class BookDetailsViewModel : ViewModelBase
    {
        private readonly AppState _appState;
        private readonly IBookService _bookService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;
        private readonly IUserLibraryService _userLibraryService;

        private Book? _book;
        private ImageSource? _coverImage;
        private bool _isLoading;
        private bool _isFavorite;
        private bool _isInLibrary;

        public BookDetailsViewModel(int bookId)
        {
            _appState = ServiceLocator.AppState;
            _bookService = ServiceLocator.BookService;
            _dialogService = ServiceLocator.DialogService;
            _navigationService = ServiceLocator.NavigationService;
            _userLibraryService = ServiceLocator.UserLibraryService;

            BackCommand = new RelayCommand(_ => _navigationService.GoBack());
            StartReadingCommand = new RelayCommand(_ => StartReading(), _ => CanRead);
            ToggleFavoriteCommand = new RelayCommand(async _ => await ToggleFavoriteAsync(), _ => IsAuthenticated && Book is not null);
            AddToLibraryCommand = new RelayCommand(async _ => await AddToLibraryAsync(), _ => CanAddToLibrary);
            ReloadCommand = new RelayCommand(async _ => await ReloadAsync(), _ => BookId > 0);

            _ = InitializeAsync(bookId);
            UpdateStateFromApp();
        }

        public int BookId { get; private set; }

        public ObservableCollection<string> Tags { get; } = new();
        public ObservableCollection<string> Categories { get; } = new();

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
            set
            {
                if (SetProperty(ref _isFavorite, value))
                    OnPropertyChanged(nameof(CanFavorite));
            }
        }

        public bool IsInLibrary
        {
            get => _isInLibrary;
            set
            {
                if (SetProperty(ref _isInLibrary, value))
                    OnPropertyChanged(nameof(CanAddToLibrary));
            }
        }

        public bool IsAuthenticated => _appState?.IsAuthenticated ?? false;
        public bool CanRead => Book is not null && IsAuthenticated;
        public bool CanFavorite => Book is not null && IsAuthenticated;
        public bool CanAddToLibrary => Book is not null && IsAuthenticated && !_isInLibrary;

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
        public string ChaptersCountText => Book is null ? "—" : Book.ChaptersCount.ToString(CultureInfo.CurrentCulture);
        public string SymbolsCountText => Book is null ? "—" : FormatNumber(Book.SymbolsCount);

        public ICommand BackCommand { get; }
        public ICommand StartReadingCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand AddToLibraryCommand { get; }
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

                if (_bookService == null) throw new InvalidOperationException("Критическая ошибка: Сервис книг не найден.");

                var book = await _bookService.GetByIdAsync(BookId);
                Book = book;

                LoadCollections(book);
                CoverImage = LoadImageSource(book?.CoverImagePath);

                if (IsAuthenticated && _userLibraryService != null)
                {
                    IsInLibrary = await _userLibraryService.IsBookInLibraryAsync(BookId);
                    IsFavorite = await _userLibraryService.IsBookFavoriteAsync(BookId);
                }
                else
                {
                    IsInLibrary = false;
                    IsFavorite = false;
                }
            }
            catch (Exception ex)
            {
                if (_dialogService != null)
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
                    if (!string.IsNullOrEmpty(tag?.Name)) Tags.Add(tag.Name);
            }

            if (book.Categories != null)
            {
                foreach (var category in book.Categories)
                    if (!string.IsNullOrEmpty(category?.Name)) Categories.Add(category.Name);
            }
        }

        private void UpdateStateFromApp()
        {
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(CanRead));
            OnPropertyChanged(nameof(CanAddToLibrary));
            OnPropertyChanged(nameof(CanFavorite));
        }

        private void StartReading()
        {
            if (Book is not null)
                _navigationService.Navigate(new ReaderViewModel(Book.BookId));
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

        private async Task AddToLibraryAsync()
        {
            if (Book is null) return;

            try
            {
                await _userLibraryService.AddToLibraryAsync(Book.BookId);
                IsInLibrary = true;
                await _dialogService.ShowMessageAsync("Готово", "Книга добавлена в библиотеку.");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
            }
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