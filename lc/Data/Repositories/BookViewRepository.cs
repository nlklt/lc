using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class BookViewRepository : IBookViewRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public BookViewRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<bool> ExistsAsync(int userId, int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.BookViews.AnyAsync(x => x.UserId == userId && x.BookId == bookId);
    }

    public async Task<int> CountAsync(int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.BookViews.CountAsync(x => x.BookId == bookId);
    }

    public async Task AddAsync(int userId, int bookId, DateTime viewedAt)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var book = await db.Books.FirstOrDefaultAsync(x => x.BookId == bookId);
        if (book is null)
            throw new InvalidOperationException($"Книга с BookId={bookId} не найдена.");

        var existing = await db.BookViews
            .FirstOrDefaultAsync(x => x.UserId == userId && x.BookId == bookId);

        var stamp = viewedAt == default ? DateTime.Now : viewedAt;

        if (existing is null)
        {
            db.BookViews.Add(new BookView
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

        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<BookView>> GetByBookIdAsync(int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.BookViews
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.BookId == bookId)
            .OrderByDescending(x => x.ViewedAt)
            .ToListAsync();
    }
}