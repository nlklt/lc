using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class UserLibraryListRepository : IUserLibraryListRepository
{
    private static readonly string[] DefaultListNames =
    [
        "Читаю",
        "В планах",
        "Брошено"
    ];

    private readonly AppDbContext _db;

    public UserLibraryListRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserLibraryListDto>> GetListsAsync(int userId)
    {
        return await _db.UserLibraryLists
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name == "Читаю" ? 0 :
                          x.Name == "В планах" ? 1 :
                          x.Name == "Брошено" ? 2 : 100)
            .ThenBy(x => x.Name)
            .Select(x => new UserLibraryListDto
            {
                ListId = x.ListId,
                Name = x.Name
            })
            .ToListAsync();
    }

    public async Task<UserLibraryList?> GetByIdAsync(int userId, int listId)
    {
        return await _db.UserLibraryLists
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ListId == listId);
    }

    public async Task<int> CreateAsync(int userId, string name)
    {
        var normalizedName = NormalizeName(name);

        var existing = await _db.UserLibraryLists
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Name == normalizedName);

        if (existing is not null)
            return existing.ListId;

        var list = new UserLibraryList
        {
            UserId = userId,
            Name = normalizedName
        };

        _db.UserLibraryLists.Add(list);
        await _db.SaveChangesAsync();

        return list.ListId;
    }

    public async Task RenameAsync(int userId, int listId, string name)
    {
        var list = await _db.UserLibraryLists
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ListId == listId);

        if (list is null)
            return;

        list.Name = NormalizeName(name);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int userId, int listId)
    {
        var list = await _db.UserLibraryLists
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ListId == listId);

        if (list is null)
            return;

        _db.UserLibraryLists.Remove(list);
        await _db.SaveChangesAsync();
    }

    public async Task EnsureDefaultListsAsync(int userId)
    {
        var existingNames = await _db.UserLibraryLists
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Name)
            .ToListAsync();

        foreach (var name in DefaultListNames)
        {
            if (existingNames.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
                continue;

            _db.UserLibraryLists.Add(new UserLibraryList
            {
                UserId = userId,
                Name = name
            });
        }

        await _db.SaveChangesAsync();
    }

    private static string NormalizeName(string name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? "Без названия"
            : name.Trim();
    }
}