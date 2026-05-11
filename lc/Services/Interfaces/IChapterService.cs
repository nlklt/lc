using lc.Models;

namespace lc.Services.Interfaces
{
    public interface IChapterService
    {
        Task<List<Chapter>> GetByBookIdAsync(int bookId);
    }
}