using lc.Helpers;

namespace lc.Data.Repositories.Interfaces
{
    public interface IUserLibraryListBookRepository
    {
        Task<IReadOnlyList<BookListItemDto>> GetBooksAsync(int userId, int listId);

        Task AddBookAsync(int userId, int listId, int bookId);
        Task RemoveBookAsync(int userId, int listId, int bookId);
        Task<bool> ExistsAsync(int userId, int listId, int bookId);
    }
}
