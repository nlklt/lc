using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class CommentRepository : ICommentRepository
{
    private readonly AppDbContext _db;

    public CommentRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Comment?> GetByIdAsync(int commentId)
    {
        return await _db.Comments
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.CommentId == commentId);
    }

    public async Task<IReadOnlyList<Comment>> GetByBookIdAsync(int bookId)
    {
        return await _db.Comments
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.BookId == bookId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(Comment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);

        if (comment.CreatedAt == default)
            comment.CreatedAt = DateTime.Now;

        comment.UpdatedAt = DateTime.Now;

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        return comment.CommentId;
    }

    public async Task UpdateAsync(Comment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);

        var existing = await _db.Comments
            .FirstOrDefaultAsync(x => x.CommentId == comment.CommentId)
            ?? throw new InvalidOperationException($"Комментарий с CommentId={comment.CommentId} не найден.");

        var createdAt = existing.CreatedAt;

        _db.Entry(existing).CurrentValues.SetValues(comment);
        existing.CreatedAt = createdAt;
        existing.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int commentId)
    {
        var comment = await _db.Comments
            .FirstOrDefaultAsync(x => x.CommentId == commentId);

        if (comment is null)
            return;

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();
    }
}