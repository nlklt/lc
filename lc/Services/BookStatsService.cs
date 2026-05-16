using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Infrastructure.Repositories.Sql;
using lc.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace lc.Services;

public sealed class BookStatsService : IBookStatsService
{
    private readonly AppState _appState;
    private readonly IBookRepository _bookRepository;
    private readonly IBookViewRepository _bookViewRepository;
    private readonly IBookRatingRepository _bookRatingRepository;
    private readonly IChapterRepository _chapterRepository;

    public BookStatsService(
        AppState appState,
        IBookRepository bookRepository,
        IBookViewRepository bookViewRepository,
        IBookRatingRepository bookRatingRepository,
        IChapterRepository chapterRepository)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
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

    private int CurrentUserId =>
        _appState.CurrentUser?.UserId
        ?? throw new InvalidOperationException("Действие невозможно: пользователь не авторизован.");
}