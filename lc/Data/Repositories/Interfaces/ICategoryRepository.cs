using lc.Models;

namespace lc.Data.Repositories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<IReadOnlyList<Category>> GetAllAsync();
    }
}
