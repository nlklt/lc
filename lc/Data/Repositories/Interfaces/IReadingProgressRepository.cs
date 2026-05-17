using lc.Models;

namespace lc.Data.Repositories.Interfaces
{
    public interface IReadingProgressRepository
    {
        Task<ReadingProgress?> GetAsync(int userId, int chapterId);
        Task<IReadOnlyList<ReadingProgress>> GetByUserIdAsync(int userId);
        Task<IReadOnlyList<ReadingProgress>> GetByChapterIdAsync(int chapterId);
        Task<ReadingProgress?> GetLastByBookAsync(int userId, int bookId);

        Task AddOrUpdateAsync(ReadingProgress progress);
        Task DeleteAsync(int userId, int chapterId);
    }
}
