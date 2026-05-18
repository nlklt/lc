using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class ChapterRepository : IChapterRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ChapterRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<Chapter?> GetByIdAsync(int chapterId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Chapters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ChapterId == chapterId);
    }

    public async Task<IReadOnlyList<Chapter>> GetByBookIdAsync(int bookId, bool includeDrafts = true)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var query = db.Chapters
            .AsNoTracking()
            .Where(x => x.BookId == bookId);

        if (!includeDrafts)
            query = query.Where(x => x.Status == ChapterStatus.Published);

        return await query
            .OrderBy(x => x.ChapterNumber)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(Chapter chapter)
    {
        ArgumentNullException.ThrowIfNull(chapter);

        await using var db = await _dbFactory.CreateDbContextAsync();

        if (chapter.CreatedAt == default)
            chapter.CreatedAt = DateTime.Now;

        chapter.UpdatedAt = DateTime.Now;

        db.Chapters.Add(chapter);
        await db.SaveChangesAsync();

        return chapter.ChapterId;
    }

    public async Task UpdateAsync(Chapter chapter)
    {
        ArgumentNullException.ThrowIfNull(chapter);

        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.Chapters
            .FirstOrDefaultAsync(x => x.ChapterId == chapter.ChapterId)
            ?? throw new InvalidOperationException($"Глава с ChapterId={chapter.ChapterId} не найдена.");

        var createdAt = existing.CreatedAt;

        db.Entry(existing).CurrentValues.SetValues(chapter);
        existing.CreatedAt = createdAt;
        existing.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int chapterId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var chapter = await db.Chapters
            .FirstOrDefaultAsync(x => x.ChapterId == chapterId);

        if (chapter is null)
            return;

        db.Chapters.Remove(chapter);
        await db.SaveChangesAsync();
    }
}