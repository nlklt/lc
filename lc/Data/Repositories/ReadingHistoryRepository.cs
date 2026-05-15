using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class ReadingHistoryRepository : IReadingHistoryRepository
{
    private readonly AppDbContext _db;

    public ReadingHistoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReadingHistory?> GetLatestAsync(int userId, int bookId)
    {
        return await _db.ReadingHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.BookId == bookId);
    }

    public async Task<IReadOnlyList<ReadingHistory>> GetByUserIdAsync(int userId)
    {
        return await _db.ReadingHistories
            .AsNoTracking()
            .Include(x => x.Book)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastOpenedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ReadingHistory>> GetByBookIdAsync(int bookId)
    {
        return await _db.ReadingHistories
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.BookId == bookId)
            .OrderByDescending(x => x.LastOpenedAt)
            .ToListAsync();
    }

    public async Task AddOrUpdateAsync(int userId, int bookId, DateTime lastOpenedAt)
    {
        var existing = await _db.ReadingHistories
            .FirstOrDefaultAsync(x => x.UserId == userId && x.BookId == bookId);

        var stamp = lastOpenedAt == default ? DateTime.Now : lastOpenedAt;

        if (existing is null)
        {
            _db.ReadingHistories.Add(new ReadingHistory
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

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int historyId)
    {
        var history = await _db.ReadingHistories
            .FirstOrDefaultAsync(x => x.HistoryId == historyId);

        if (history is null)
            return;

        _db.ReadingHistories.Remove(history);
        await _db.SaveChangesAsync();
    }
}