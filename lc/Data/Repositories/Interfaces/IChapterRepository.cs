using lc.Models;

namespace lc.Infrastructure.Repositories.Abstractions
{
    public interface IChapterRepository
    {
        Task<Chapter?> GetByIdAsync(int chapterId);
        Task<List<Chapter>> GetByBookIdAsync(int bookId);
        Task<int> CreateAsync(Chapter chapter);
        Task UpdateAsync(Chapter chapter);
        Task DeleteAsync(int chapterId);
    }
}