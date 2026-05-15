using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Data.Repositories;

public sealed class TagRepository : ITagRepository
{
    private readonly AppDbContext _db;

    public TagRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Tag>> GetAllAsync()
    {
        return await _db.Tags
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }
}