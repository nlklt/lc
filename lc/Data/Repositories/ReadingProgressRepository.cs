using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class ReadingProgressRepository : IReadingProgressRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ReadingProgressRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
    }

    public async Task<ReadingProgress?> GetAsync(int userId, int chapterId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.ReadingProgresses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ChapterId == chapterId);
    }

    public async Task<ReadingProgress?> GetLastByBookAsync(int userId, int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.ReadingProgresses
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.BookId == bookId)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<ReadingProgress>> GetByUserIdAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.ReadingProgresses
            .AsNoTracking()
            .Include(x => x.Chapter)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ReadingProgress>> GetByChapterIdAsync(int chapterId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.ReadingProgresses
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.ChapterId == chapterId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();
    }

    public async Task AddOrUpdateAsync(ReadingProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        if (progress.UserId <= 0)
            throw new ArgumentOutOfRangeException(nameof(progress.UserId));

        if (progress.BookId <= 0)
            throw new ArgumentOutOfRangeException(nameof(progress.BookId));

        if (progress.ChapterId <= 0)
            throw new ArgumentOutOfRangeException(nameof(progress.ChapterId));

        if (progress.ProgressPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(progress.ProgressPercent), "ProgressPercent должен быть в диапазоне от 0 до 100.");

        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.ReadingProgresses
            .FirstOrDefaultAsync(x => x.UserId == progress.UserId && x.ChapterId == progress.ChapterId);

        if (existing is null)
        {
            if (progress.UpdatedAt == default)
                progress.UpdatedAt = DateTime.Now;

            db.ReadingProgresses.Add(progress);
        }
        else
        {
            existing.BookId = progress.BookId;
            existing.ProgressPercent = progress.ProgressPercent;
            existing.LastPosition = progress.LastPosition;
            existing.UpdatedAt = DateTime.Now;
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            var raceWinner = await db.ReadingProgresses
                .FirstOrDefaultAsync(x => x.UserId == progress.UserId && x.ChapterId == progress.ChapterId);

            if (raceWinner is null)
                throw;

            raceWinner.BookId = progress.BookId;
            raceWinner.ProgressPercent = progress.ProgressPercent;
            raceWinner.LastPosition = progress.LastPosition;
            raceWinner.UpdatedAt = DateTime.Now;

            await db.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int userId, int chapterId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var progress = await db.ReadingProgresses
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ChapterId == chapterId);

        if (progress is null)
            return;

        db.ReadingProgresses.Remove(progress);
        await db.SaveChangesAsync();
    }
}