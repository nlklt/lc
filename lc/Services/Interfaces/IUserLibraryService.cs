using lc.Models;

namespace lc.Services.Interfaces
{
    public interface IUserLibraryService
    {
        Task<IReadOnlyList<UserLibraryListDto>> GetListsAsync(int userId);
        Task AddBookToListAsync(int userId, int listId, int bookId);
        Task RemoveBookFromListAsync(int userId, int listId, int bookId);
        Task<IReadOnlyList<BookListItem>> GetBooksFromListAsync(int userId, int listId);

        Task<bool> IsBookInLibraryAsync(int bookId);
        Task<bool> IsBookFavoriteAsync(int bookId);

        Task AddToLibraryAsync(int bookId);
        Task AddToFavoritesAsync(int bookId);
        Task RemoveFromFavoritesAsync(int bookId);
    }

    public sealed class UserLibraryListDto
    {
        public int ListId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}