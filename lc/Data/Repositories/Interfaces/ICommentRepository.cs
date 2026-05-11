using lc.Models;

namespace lc.Infrastructure.Repositories.Abstractions
{
    public interface ICommentRepository
    {
        Task<int> CreateAsync(Comment comment);
        Task<Comment?> GetByIdAsync(int commentId);
        Task<IReadOnlyList<Comment>> GetByBookIdAsync(int bookId);
        Task<IReadOnlyList<Comment>> GetByChapterIdAsync(int chapterId);
        Task UpdateAsync(Comment comment);
        Task DeleteAsync(int commentId);

    }
}