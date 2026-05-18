using lc.Data.Repositories.Interfaces;
using lc.Helpers;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace lc.Services;

public sealed class BookStatsService : IBookStatsService
{
    private readonly AppState _appState;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IBookRepository _bookRepository;
    private readonly IBookViewRepository _bookViewRepository;
    private readonly IBookRatingRepository _bookRatingRepository;
    private readonly IChapterRepository _chapterRepository;

    public BookStatsService(
        AppState appState,
        IDbContextFactory<AppDbContext> dbFactory,
        IBookRepository bookRepository,
        IBookViewRepository bookViewRepository,
        IBookRatingRepository bookRatingRepository,
        IChapterRepository chapterRepository)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
        _bookViewRepository = bookViewRepository ?? throw new ArgumentNullException(nameof(bookViewRepository));
        _bookRatingRepository = bookRatingRepository ?? throw new ArgumentNullException(nameof(bookRatingRepository));
        _chapterRepository = chapterRepository ?? throw new ArgumentNullException(nameof(chapterRepository));
    }

    public async Task RegisterViewAsync(int bookId)
    {
        var userId = CurrentUserId;

        if (!await _bookViewRepository.ExistsAsync(userId, bookId))
        {
            await _bookViewRepository.AddAsync(userId, bookId, DateTime.Now);
        }

        await RecalculateBookCacheAsync(bookId);
    }

    public async Task SetRatingAsync(int bookId, byte rating)
    {
        var userId = CurrentUserId;

        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Рейтинг должен быть от 1 до 5.");

        await _bookRatingRepository.AddOrUpdateAsync(userId, bookId, rating);
        await RecalculateBookCacheAsync(bookId);
    }

    public async Task RecalculateBookCacheAsync(int bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId, includeChapters: true)
            ?? throw new InvalidOperationException($"Книга с BookId={bookId} не найдена.");

        book.Views = await _bookViewRepository.CountAsync(bookId);
        book.Rating = await _bookRatingRepository.GetAverageRatingAsync(bookId);

        var chapters = await _chapterRepository.GetByBookIdAsync(bookId, includeDrafts: false);

        book.ChaptersCount = chapters.Count;
        book.SymbolsCount = chapters.Sum(x => (long)x.Text.Length);

        await _bookRepository.UpdateAsync(book);
    }

    public async Task<IReadOnlyList<BookDailyStatsPointDto>> GetBookDailyStatsAsync(int bookId, int days = 30)
    {
        if (days < 1)
            throw new ArgumentOutOfRangeException(nameof(days));

        var start = DateTime.Today.AddDays(-(days - 1));
        var end = DateTime.Today.AddDays(1);

        await using var db = await _dbFactory.CreateDbContextAsync();

        var views = await db.BookViews
            .AsNoTracking()
            .Where(x => x.BookId == bookId && x.ViewedAt >= start && x.ViewedAt < end)
            .GroupBy(x => x.ViewedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync();

        var ratings = await db.BookRatings
            .AsNoTracking()
            .Where(x => x.BookId == bookId && x.RatedAt >= start && x.RatedAt < end)
            .GroupBy(x => x.RatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync();

        var comments = await db.Comments
            .AsNoTracking()
            .Where(x => x.BookId == bookId && x.CreatedAt >= start && x.CreatedAt < end)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync();

        var viewMap = views.ToDictionary(x => x.Day.Date, x => x.Count);
        var ratingMap = ratings.ToDictionary(x => x.Day.Date, x => x.Count);
        var commentMap = comments.ToDictionary(x => x.Day.Date, x => x.Count);

        var result = new List<BookDailyStatsPointDto>(days);

        for (var i = 0; i < days; i++)
        {
            var day = start.Date.AddDays(i);

            viewMap.TryGetValue(day, out var viewCount);
            ratingMap.TryGetValue(day, out var ratingCount);
            commentMap.TryGetValue(day, out var commentCount);

            result.Add(new BookDailyStatsPointDto(day, viewCount, ratingCount, commentCount));
        }

        return result;
    }

    private int CurrentUserId =>
        _appState.CurrentUser?.UserId
        ?? throw new InvalidOperationException("Действие невозможно: пользователь не авторизован.");
}