using lc.Models;
using lc.Services.Interfaces;

namespace lc.Data.Repositories.Interfaces
{
    public interface IUserLibraryRepository
    {
        public Task<IReadOnlyList<UserLibraryListDto>> GetListsAsync(int userId);
        public Task<IReadOnlyList<BookListItem>> GetBooksFromListAsync(int userId, int listId);
        public Task AddBookToListAsync(int userId, int listId, int bookId);
        public Task RemoveBookFromListAsync(int userId, int listId, int bookId);
        public Task AddToFavoritesAsync(int userId, int bookId);
        public Task RemoveFromFavoritesAsync(int userId, int bookId);
        public Task<bool> IsBookFavoriteAsync(int userId, int bookId);
    }
}
