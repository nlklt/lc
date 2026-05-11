using lc.Models;

namespace lc.Data.Repositories.Interfaces
{
    public interface IUserLibraryService
    {
        Task<IReadOnlyList<UserLibraryListDto>> GetListsAsync(int userId);
        Task AddBookToListAsync(int userId, int listId, int bookId);
        Task RemoveBookFromListAsync(int userId, int listId, int bookId);
        Task<IReadOnlyList<BookListItem>> GetBooksFromListAsync(int userId, int listId);
    }

    public sealed class UserLibraryListDto
    {
        public int ListId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}