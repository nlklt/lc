using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class ReadingProgressRepository : IReadingProgressRepository
{
    private readonly AppDbContext _db;

    public ReadingProgressRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReadingProgress?> GetAsync(int userId, int chapterId)
    {
        return await _db.ReadingProgresses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ChapterId == chapterId);
    }

    public async Task<IReadOnlyList<ReadingProgress>> GetByUserIdAsync(int userId)
    {
        return await _db.ReadingProgresses
            .AsNoTracking()
            .Include(x => x.Chapter)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ReadingProgress>> GetByChapterIdAsync(int chapterId)
    {
        return await _db.ReadingProgresses
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.ChapterId == chapterId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();
    }

    public async Task AddOrUpdateAsync(ReadingProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        if (progress.ProgressPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(progress.ProgressPercent), "ProgressPercent должен быть в диапазоне от 0 до 100.");

        var existing = await _db.ReadingProgresses
            .FirstOrDefaultAsync(x => x.UserId == progress.UserId && x.ChapterId == progress.ChapterId);

        if (existing is null)
        {
            if (progress.UpdatedAt == default)
                progress.UpdatedAt = DateTime.Now;

            _db.ReadingProgresses.Add(progress);
        }
        else
        {
            existing.ProgressPercent = progress.ProgressPercent;
            existing.LastPosition = progress.LastPosition;
            existing.UpdatedAt = DateTime.Now;
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int userId, int chapterId)
    {
        var progress = await _db.ReadingProgresses
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ChapterId == chapterId);

        if (progress is null)
            return;

        _db.ReadingProgresses.Remove(progress);
        await _db.SaveChangesAsync();
    }
}