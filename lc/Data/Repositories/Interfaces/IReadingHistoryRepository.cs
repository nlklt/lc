using lc.Models;

namespace lc.Data.Repositories.Interfaces
{
    public interface IReadingHistoryRepository
    {
        Task<ReadingHistory?> GetLatestAsync(int userId, int bookId);
        Task<IReadOnlyList<ReadingHistory>> GetByUserIdAsync(int userId);
        Task<IReadOnlyList<ReadingHistory>> GetByBookIdAsync(int bookId);

        Task AddOrUpdateAsync(int userId, int bookId, DateTime lastOpenedAt);
        Task DeleteAsync(int historyId);
    }
}
