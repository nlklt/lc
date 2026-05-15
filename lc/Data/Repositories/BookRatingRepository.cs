using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class BookRatingRepository : IBookRatingRepository
{
    private readonly AppDbContext _db;

    public BookRatingRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<BookRating?> GetAsync(int userId, int bookId)
    {
        return await _db.BookRatings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.BookId == bookId);
    }

    public async Task<IReadOnlyList<BookRating>> GetByBookIdAsync(int bookId)
    {
        return await _db.BookRatings
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.BookId == bookId)
            .OrderByDescending(x => x.RatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<BookRating>> GetByUserIdAsync(int userId)
    {
        return await _db.BookRatings
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

        var bookExists = await _db.Books.AnyAsync(x => x.BookId == bookId);
        if (!bookExists)
            throw new InvalidOperationException($"Книга с BookId={bookId} не найдена.");

        var existing = await _db.BookRatings
            .FirstOrDefaultAsync(x => x.UserId == userId && x.BookId == bookId);

        if (existing is null)
        {
            _db.BookRatings.Add(new BookRating
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

        await _db.SaveChangesAsync();
        await RecalculateBookRatingAsync(bookId);
    }

    public async Task RemoveAsync(int userId, int bookId)
    {
        var rating = await _db.BookRatings
            .FirstOrDefaultAsync(x => x.UserId == userId && x.BookId == bookId);

        if (rating is null)
            return;

        _db.BookRatings.Remove(rating);
        await _db.SaveChangesAsync();

        await RecalculateBookRatingAsync(bookId);
    }

    public async Task<decimal> GetAverageRatingAsync(int bookId)
    {
        var average = await _db.BookRatings
            .AsNoTracking()
            .Where(x => x.BookId == bookId)
            .AverageAsync(x => (decimal?)x.Rating);

        return average ?? 0m;
    }

    private async Task RecalculateBookRatingAsync(int bookId)
    {
        var book = await _db.Books.FirstOrDefaultAsync(x => x.BookId == bookId);
        if (book is null)
            return;

        book.Rating = await GetAverageRatingAsync(bookId);
        await _db.SaveChangesAsync();
    }
}