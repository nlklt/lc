using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
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
        "Брошено",
        "Прочитано"
    ];

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public UserLibraryListRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<UserLibraryListDto>> GetListsAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.UserLibraryLists
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name == "Читаю" ? 0 :
                          x.Name == "В планах" ? 1 :
                          x.Name == "Брошено" ? 2 :
                          x.Name == "Прочитано" ? 3 : 100)
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
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.UserLibraryLists
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ListId == listId);
    }

    public async Task<int> CreateAsync(int userId, string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var normalizedName = NormalizeName(name);

        var existing = await db.UserLibraryLists
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Name == normalizedName);

        if (existing is not null)
            return existing.ListId;

        var list = new UserLibraryList
        {
            UserId = userId,
            Name = normalizedName
        };

        db.UserLibraryLists.Add(list);
        await db.SaveChangesAsync();

        return list.ListId;
    }

    public async Task RenameAsync(int userId, int listId, string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var list = await db.UserLibraryLists
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ListId == listId);

        if (list is null)
            return;

        if (IsProtectedList(list.Name))
            throw new InvalidOperationException("Системный список нельзя переименовать.");

        list.Name = NormalizeName(name);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int userId, int listId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var list = await db.UserLibraryLists
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ListId == listId);

        if (list is null)
            return;

        if (IsProtectedList(list.Name))
            throw new InvalidOperationException("Системный список нельзя удалить.");

        db.UserLibraryLists.Remove(list);
        await db.SaveChangesAsync();
    }

    public async Task EnsureDefaultListsAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existingNames = (await db.UserLibraryLists
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Name)
            .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var name in DefaultListNames)
        {
            if (existingNames.Contains(name))
                continue;

            db.UserLibraryLists.Add(new UserLibraryList
            {
                UserId = userId,
                Name = name
            });
        }

        await db.SaveChangesAsync();
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название списка не может быть пустым.", nameof(name));

        return name.Trim();
    }

    private static bool IsProtectedList(string name)
        => DefaultListNames.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
}