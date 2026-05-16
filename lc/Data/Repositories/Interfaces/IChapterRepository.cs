using lc.Models;

namespace lc.Infrastructure.Repositories.Abstractions
{
    public interface IChapterRepository
    {
        Task<Chapter?> GetByIdAsync(int chapterId);
        Task<IReadOnlyList<Chapter>> GetByBookIdAsync(int bookId, bool includeDrafts = true);

        Task<int> CreateAsync(Chapter chapter);
        Task UpdateAsync(Chapter chapter);
        Task DeleteAsync(int chapterId);
    }
}