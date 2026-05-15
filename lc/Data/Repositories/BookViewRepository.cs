using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class BookViewRepository : IBookViewRepository
{
    private readonly AppDbContext _db;

    public BookViewRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ExistsAsync(int userId, int bookId)
    {
        return await _db.BookViews.AnyAsync(x => x.UserId == userId && x.BookId == bookId);
    }

    public async Task<int> CountAsync(int bookId)
    {
        return await _db.BookViews.CountAsync(x => x.BookId == bookId);
    }

    public async Task AddAsync(int userId, int bookId, DateTime viewedAt)
    {
        var book = await _db.Books.FirstOrDefaultAsync(x => x.BookId == bookId);
        if (book is null)
            throw new InvalidOperationException($"Книга с BookId={bookId} не найдена.");

        var existing = await _db.BookViews
            .FirstOrDefaultAsync(x => x.UserId == userId && x.BookId == bookId);

        var stamp = viewedAt == default ? DateTime.Now : viewedAt;

        if (existing is null)
        {
            _db.BookViews.Add(new BookView
            {
                UserId = userId,
                BookId = bookId,
                ViewedAt = stamp
            });

            book.Views++;
        }
        else
        {
            existing.ViewedAt = stamp;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<BookView>> GetByBookIdAsync(int bookId)
    {
        return await _db.BookViews
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.BookId == bookId)
            .OrderByDescending(x => x.ViewedAt)
            .ToListAsync();
    }
}