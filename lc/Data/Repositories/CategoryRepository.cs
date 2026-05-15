using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Data.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        return await _db.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }
}