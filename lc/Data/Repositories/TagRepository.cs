using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Data.Repositories;

public sealed class TagRepository : ITagRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public TagRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<Tag>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Tags
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }
}