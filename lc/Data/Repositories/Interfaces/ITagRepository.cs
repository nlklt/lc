using lc.Models;

namespace lc.Data.Repositories.Interfaces
{
    public interface ITagRepository
    {
        public Task<IReadOnlyList<Tag>> GetAllAsync();
    }
}
