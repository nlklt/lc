using lc.Models;

namespace lc.Data.Repositories.Interfaces
{
    public interface IBookViewRepository
    {
        Task<bool> ExistsAsync(int userId, int bookId);
        Task<int> CountAsync(int bookId);

        Task AddAsync(int userId, int bookId, DateTime viewedAt);
        Task<IReadOnlyList<BookView>> GetByBookIdAsync(int bookId);
    }
}
