using lc.Models;

namespace lc.Services.Interfaces
{
    public interface ICommentService
    {
        Task<IReadOnlyList<Comment>> GetByBookIdAsync(int bookId);
        Task<int> AddAsync(int bookId, string text);
        Task UpdateAsync(Comment comment);
        Task DeleteAsync(int commentId);
    }
}