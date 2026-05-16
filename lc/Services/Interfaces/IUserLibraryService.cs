using lc.Helpers;

namespace lc.Services.Interfaces
{
    public interface IUserLibraryService
    {
        Task<IReadOnlyList<UserLibraryListDto>> GetListsAsync();
        Task<int> CreateListAsync(string name);
        Task RenameListAsync(int listId, string name);
        Task DeleteListAsync(int listId);

        Task<IReadOnlyList<BookListItemDto>> GetBooksFromListAsync(int listId);
        Task AddBookToListAsync(int listId, int bookId);
        Task RemoveBookFromListAsync(int listId, int bookId);

        Task<bool> IsBookInLibraryAsync(int bookId);

        Task EnsureDefaultListsAsync();
    }

    public sealed class UserLibraryListDto
    {
        public int ListId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}