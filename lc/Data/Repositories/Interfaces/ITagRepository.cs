using lc.Models;

namespace lc.Data.Repositories.Interfaces
{
    public interface ITagRepository
    {
        Task<IReadOnlyList<Tag>> GetAllAsync();
    }
}
