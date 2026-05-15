using lc.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lc.Data.Repositories.Interfaces
{
    public interface IFavoriteRepository
    {
        Task AddAsync(int userId, int bookId);
        Task RemoveAsync(int userId, int bookId);
        Task<bool> ExistsAsync(int userId, int bookId);
        Task<IReadOnlyList<BookListItemDto>> GetByUserIdAsync(int userId);
    }
}
