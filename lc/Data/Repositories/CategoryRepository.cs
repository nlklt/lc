using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Data.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CategoryRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }
}