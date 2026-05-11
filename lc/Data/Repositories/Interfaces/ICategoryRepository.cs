using lc.Models;

namespace lc.Data.Repositories.Interfaces
{
    public interface ICategoryRepository
    {
        public Task<IReadOnlyList<Category>> GetAllAsync();
    }
}
