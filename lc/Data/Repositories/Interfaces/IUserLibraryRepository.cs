using lc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lc.Data.Repositories.Interfaces
{
    public interface IUserLibraryRepository
    {
        public Task<IReadOnlyList<UserLibraryListDto>> GetListsAsync(int userId);
        public Task AddBookToListAsync(int userId, int listId, int bookId);
        public Task RemoveBookFromListAsync(int userId, int listId, int bookId);
        public Task<IReadOnlyList<BookListItem>> GetBooksFromListAsync(int userId, int listId);
    }
}
