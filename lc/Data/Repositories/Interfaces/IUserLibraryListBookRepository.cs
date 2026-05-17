using lc.Helpers;
using lc.Models;

namespace lc.Infrastructure.Repositories.Abstractions;

public interface IUserLibraryListBookRepository
{
    Task<IReadOnlyList<BookListItemDto>> GetBooksAsync(int userId, int listId);
    Task<bool> ExistsAsync(int userId, int listId, int bookId);
    Task<bool> ExistsInAnyListAsync(int userId, int bookId);
    Task<bool> AddBookAsync(int userId, int listId, int bookId);
    Task RemoveBookAsync(int userId, int listId, int bookId);
}