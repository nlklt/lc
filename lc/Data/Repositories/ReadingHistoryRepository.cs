using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class ReadingHistoryRepository : IReadingHistoryRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ReadingHistoryRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<ReadingHistory?> GetLatestAsync(int userId, int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.ReadingHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.BookId == bookId);
    }

    public async Task<IReadOnlyList<ReadingHistory>> GetByUserIdAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.ReadingHistories
            .AsNoTracking()
            .Include(x => x.Book)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastOpenedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ReadingHistory>> GetByBookIdAsync(int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.ReadingHistories
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.BookId == bookId)
            .OrderByDescending(x => x.LastOpenedAt)
            .ToListAsync();
    }

    public async Task AddOrUpdateAsync(int userId, int bookId, DateTime lastOpenedAt)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.ReadingHistories
            .FirstOrDefaultAsync(x => x.UserId == userId && x.BookId == bookId);

        var stamp = lastOpenedAt == default ? DateTime.Now : lastOpenedAt;

        if (existing is null)
        {
            db.ReadingHistories.Add(new ReadingHistory
            {
                UserId = userId,
                BookId = bookId,
                LastOpenedAt = stamp
            });
        }
        else
        {
            existing.LastOpenedAt = stamp;
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int historyId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var history = await db.ReadingHistories
            .FirstOrDefaultAsync(x => x.HistoryId == historyId);

        if (history is null)
            return;

        db.ReadingHistories.Remove(history);
        await db.SaveChangesAsync();
    }
}