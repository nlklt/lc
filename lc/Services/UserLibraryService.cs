using lc.Models;
using lc.Services.Interfaces;

namespace lc.Services
{
    public class UserLibraryService : IUserLibraryService
    {
        public Task<bool> IsBookInLibraryAsync(int bookId)
        {
            return Task.FromResult(false);
        }

        public Task<bool> IsBookFavoriteAsync(int bookId)
        {
            return Task.FromResult(false);
        }

        public Task AddToLibraryAsync(int bookId)
        {
            return Task.CompletedTask;
        }

        public Task AddToFavoritesAsync(int bookId)
        {
            return Task.CompletedTask;
        }

        public Task RemoveFromFavoritesAsync(int bookId)
        {
            return Task.CompletedTask;
        }

        Task<IReadOnlyList<UserLibraryListDto>> IUserLibraryService.GetListsAsync(int userId)
        {
            throw new NotImplementedException();
        }

        Task IUserLibraryService.AddBookToListAsync(int userId, int listId, int bookId)
        {
            throw new NotImplementedException();
        }

        Task IUserLibraryService.RemoveBookFromListAsync(int userId, int listId, int bookId)
        {
            throw new NotImplementedException();
        }

        Task<IReadOnlyList<BookListItem>> IUserLibraryService.GetBooksFromListAsync(int userId, int listId)
        {
            throw new NotImplementedException();
        }
    }
}