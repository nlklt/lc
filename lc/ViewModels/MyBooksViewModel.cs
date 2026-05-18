using lc.Commands;
using lc.Helpers;
using lc.Infrastructure;
using lc.Services;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace lc.ViewModels;

public sealed class MyBooksViewModel : ViewModelBase
{
    private const double MaxBarHeight = 140.0;

    private readonly AppState _appState;
    private readonly IBookService _bookService;
    private readonly IBookStatsService _bookStatsService;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialog;

    private MyBookCardDto? _selectedBook;
    private bool _isLoading;

    public ObservableCollection<MyBookCardDto> Books { get; } = new();
    public ObservableCollection<BookDailyStatsBarItemViewModel> SelectedBookStats { get; } = new();

    public MyBookCardDto? SelectedBook
    {
        get => _selectedBook;
        private set
        {
            if (SetProperty(ref _selectedBook, value))
            {
                OnPropertyChanged(nameof(SelectedBookTitle));
                OnPropertyChanged(nameof(HasSelectedBook));
            }
        }
    }

    public string SelectedBookTitle => SelectedBook?.Title ?? "Книга не выбрана";
    public bool HasSelectedBook => SelectedBook is not null;

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand SelectBookCommand { get; }
    public ICommand OpenEditBookCommand { get; }

    public MyBooksViewModel(
        AppState appState,
        IBookService bookService,
        IBookStatsService bookStatsService,
        INavigationService navigation,
        IDialogService dialog)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
        _bookStatsService = bookStatsService ?? throw new ArgumentNullException(nameof(bookStatsService));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));

        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        SelectBookCommand = new RelayCommand(p => _ = SelectBookAsync(p as MyBookCardDto));
        OpenEditBookCommand = new RelayCommand(p => OpenEditBook(p as MyBookCardDto));

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_appState.CurrentUser is null)
        {
            Books.Clear();
            SelectedBookStats.Clear();
            SelectedBook = null;
            return;
        }

        try
        {
            IsLoading = true;

            var books = await _bookService.GetAuthoredBooksAsync(_appState.CurrentUser.UserId);

            Books.Clear();
            foreach (var book in books)
                Books.Add(book);

            if (Books.Count > 0)
                await SelectBookAsync(Books[0]);
            else
            {
                SelectedBook = null;
                SelectedBookStats.Clear();
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowMessageAsync("Не удалось загрузить книги", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SelectBookAsync(MyBookCardDto? book)
    {
        if (book is null)
            return;

        try
        {
            IsLoading = true;
            SelectedBook = book;

            var points = await _bookStatsService.GetBookDailyStatsAsync(book.BookId, 30);

            var maxValue = points.Count == 0
                ? 0
                : points.Max(x => Math.Max(x.ViewsCount, Math.Max(x.RatingsCount, x.CommentsCount)));

            SelectedBookStats.Clear();

            foreach (var p in points)
            {
                SelectedBookStats.Add(new BookDailyStatsBarItemViewModel(
                    p.Day,
                    p.ViewsCount,
                    p.RatingsCount,
                    p.CommentsCount,
                    maxValue,
                    MaxBarHeight));
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowMessageAsync("Не удалось загрузить статистику", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenEditBook(MyBookCardDto? book)
    {
        if (book is null)
            return;

        _navigation.NavigateTo<EditBookViewModel>(book.BookId);
    }
}

public sealed class BookDailyStatsBarItemViewModel
{
    public BookDailyStatsBarItemViewModel(
        DateTime day,
        int viewsCount,
        int ratingsCount,
        int commentsCount,
        int maxValue,
        double maxBarHeight)
    {
        Day = day;
        ViewsCount = viewsCount;
        RatingsCount = ratingsCount;
        CommentsCount = commentsCount;

        ViewsBarHeight = Scale(viewsCount, maxValue, maxBarHeight);
        RatingsBarHeight = Scale(ratingsCount, maxValue, maxBarHeight);
        CommentsBarHeight = Scale(commentsCount, maxValue, maxBarHeight);
    }

    public DateTime Day { get; }
    public int ViewsCount { get; }
    public int RatingsCount { get; }
    public int CommentsCount { get; }

    public string DayLabel => Day.ToString("dd.MM");
    public string FullDayLabel => Day.ToString("dd MMMM yyyy");

    public double ViewsBarHeight { get; }
    public double RatingsBarHeight { get; }
    public double CommentsBarHeight { get; }

    public int TotalCount => ViewsCount + RatingsCount + CommentsCount;

    private static double Scale(int value, int maxValue, double maxBarHeight)
    {
        if (value <= 0 || maxValue <= 0)
            return 3;

        return Math.Max(4, maxBarHeight * value / maxValue);
    }
}