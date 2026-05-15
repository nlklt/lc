using lc.Models;

namespace lc.Services.Interfaces
{
    public interface IChapterService
    {
        Task<Chapter?> GetByIdAsync(int chapterId);
        Task<IReadOnlyList<Chapter>> GetByBookIdAsync(int bookId);
        Task<int> SaveAsync(int bookId, Chapter chapter);
        Task DeleteAsync(int chapterId);
    }
}