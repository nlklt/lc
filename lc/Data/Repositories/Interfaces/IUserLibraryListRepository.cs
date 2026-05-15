using lc.Models;
using lc.Services.Interfaces;

namespace lc.Data.Repositories.Interfaces
{
    public interface IUserLibraryListRepository
    {
        Task<IReadOnlyList<UserLibraryListDto>> GetListsAsync(int userId);
        Task<UserLibraryList?> GetByIdAsync(int userId, int listId);

        Task<int> CreateAsync(int userId, string name);
        Task RenameAsync(int userId, int listId, string name);
        Task DeleteAsync(int userId, int listId);
    }
}
