using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class ChapterRepository : IChapterRepository
{
    private readonly AppDbContext _db;

    public ChapterRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Chapter?> GetByIdAsync(int chapterId)
    {
        return await _db.Chapters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ChapterId == chapterId);
    }

    public async Task<IReadOnlyList<Chapter>> GetByBookIdAsync(int bookId, bool includeDrafts = true)
    {
        var query = _db.Chapters
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

        if (chapter.CreatedAt == default)
            chapter.CreatedAt = DateTime.Now;

        chapter.UpdatedAt = DateTime.Now;

        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync();

        return chapter.ChapterId;
    }

    public async Task UpdateAsync(Chapter chapter)
    {
        ArgumentNullException.ThrowIfNull(chapter);

        var existing = await _db.Chapters
            .FirstOrDefaultAsync(x => x.ChapterId == chapter.ChapterId)
            ?? throw new InvalidOperationException($"Глава с ChapterId={chapter.ChapterId} не найдена.");

        var createdAt = existing.CreatedAt;

        _db.Entry(existing).CurrentValues.SetValues(chapter);
        existing.CreatedAt = createdAt;
        existing.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int chapterId)
    {
        var chapter = await _db.Chapters
            .FirstOrDefaultAsync(x => x.ChapterId == chapterId);

        if (chapter is null)
            return;

        _db.Chapters.Remove(chapter);
        await _db.SaveChangesAsync();
    }
}