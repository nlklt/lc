using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class CommentRepository : ICommentRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CommentRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<Comment?> GetByIdAsync(int commentId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Comments
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.CommentId == commentId);
    }

    public async Task<IReadOnlyList<Comment>> GetByBookIdAsync(int bookId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Comments
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.BookId == bookId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(Comment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);

        await using var db = await _dbFactory.CreateDbContextAsync();

        if (comment.CreatedAt == default)
            comment.CreatedAt = DateTime.Now;

        comment.UpdatedAt = DateTime.Now;

        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        return comment.CommentId;
    }

    public async Task UpdateAsync(Comment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);

        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.Comments
            .FirstOrDefaultAsync(x => x.CommentId == comment.CommentId)
            ?? throw new InvalidOperationException($"Комментарий с CommentId={comment.CommentId} не найден.");

        var createdAt = existing.CreatedAt;

        db.Entry(existing).CurrentValues.SetValues(comment);
        existing.CreatedAt = createdAt;
        existing.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int commentId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var comment = await db.Comments
            .FirstOrDefaultAsync(x => x.CommentId == commentId);

        if (comment is null)
            return;

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
    }
}