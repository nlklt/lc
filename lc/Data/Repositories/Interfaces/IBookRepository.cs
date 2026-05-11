using lc.Models;
using lc.Models.Enums;

namespace lc.Data.Repositories.Interfaces
{
    public interface IBookRepository
    {
        Task<Book?> GetByIdAsync(int bookId, bool includeChapters = false, bool includeComments = false);
        
        Task<int> CreateAsync(Book book);
        Task UpdateAsync(Book book);
        Task DeleteAsync(int bookId);

        Task<IReadOnlyList<BookListItem>> SearchAsync(BookFilterCriteria criteria);
    }
}