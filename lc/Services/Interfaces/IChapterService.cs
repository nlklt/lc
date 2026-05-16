using lc.Models;
using lc.Models.Enums;

namespace lc.Services.Interfaces
{
    public interface IChapterService
    {
        Task<Chapter?> GetByIdAsync(int chapterId);
        Task<IReadOnlyList<Chapter>> GetByBookIdAsync(int bookId, bool includeDrafts);

        Task<int> SaveAsync(int bookId, Chapter chapter, ChapterStatus targetStatus);
        Task DeleteAsync(int chapterId);
    }
}