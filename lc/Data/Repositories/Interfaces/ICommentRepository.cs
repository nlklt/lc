using lc.Models;

namespace lc.Infrastructure.Repositories.Abstractions
{
    public interface ICommentRepository
    {
        Task<Comment?> GetByIdAsync(int commentId);
        Task<IReadOnlyList<Comment>> GetByBookIdAsync(int bookId);

        Task<int> CreateAsync(Comment comment);
        Task UpdateAsync(Comment comment);
        Task DeleteAsync(int commentId);
    }
}