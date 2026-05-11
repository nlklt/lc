using lc.Models;

namespace lc.Services.Interfaces
{
    public interface ICommentService
    {
        Task<List<Comment>> GetByBookIdAsync(int bookId);
        Task AddAsync(int bookId, int userId, string text, int rating);
    }
}