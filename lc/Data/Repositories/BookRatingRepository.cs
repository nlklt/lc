using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class BookRatingRepository : IBookRatingRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public BookRatingRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<BookRating?> GetAsync(int userId, int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.BookRatings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.BookId == bookId);
    }

    public async Task<IReadOnlyList<BookRating>> GetByBookIdAsync(int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.BookRatings
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.BookId == bookId)
            .OrderByDescending(x => x.RatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<BookRating>> GetByUserIdAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.BookRatings
            .AsNoTracking()
            .Include(x => x.Book)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.RatedAt)
            .ToListAsync();
    }

    public async Task AddOrUpdateAsync(int userId, int bookId, byte rating)
    {
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating должен быть в диапазоне от 1 до 5.");

        await using var db = await _dbFactory.CreateDbContextAsync();

        var bookExists = await db.Books.AnyAsync(x => x.BookId == bookId);
        if (!bookExists)
            throw new InvalidOperationException($"Книга с BookId={bookId} не найдена.");

        var existing = await db.BookRatings
            .FirstOrDefaultAsync(x => x.UserId == userId && x.BookId == bookId);

        if (existing is null)
        {
            db.BookRatings.Add(new BookRating
            {
                UserId = userId,
                BookId = bookId,
                Rating = rating,
                RatedAt = DateTime.Now
            });
        }
        else
        {
            existing.Rating = rating;
            existing.RatedAt = DateTime.Now;
        }

        await db.SaveChangesAsync();
        await RecalculateBookRatingAsync(bookId);
    }

    public async Task RemoveAsync(int userId, int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var rating = await db.BookRatings
            .FirstOrDefaultAsync(x => x.UserId == userId && x.BookId == bookId);

        if (rating is null)
            return;

        db.BookRatings.Remove(rating);
        await db.SaveChangesAsync();

        await RecalculateBookRatingAsync(bookId);
    }

    public async Task<decimal> GetAverageRatingAsync(int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await GetAverageRatingAsync(db, bookId);
    }

    private static async Task<decimal> GetAverageRatingAsync(AppDbContext db, int bookId)
    {
        var average = await db.BookRatings
            .AsNoTracking()
            .Where(x => x.BookId == bookId)
            .AverageAsync(x => (decimal?)x.Rating);

        return average ?? 0m;
    }

    private async Task RecalculateBookRatingAsync(int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var book = await db.Books.FirstOrDefaultAsync(x => x.BookId == bookId);
        if (book is null)
            return;

        book.Rating = await GetAverageRatingAsync(db, bookId);
        await db.SaveChangesAsync();
    }
}