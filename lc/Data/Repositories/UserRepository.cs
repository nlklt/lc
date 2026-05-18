using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Data.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public UserRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<int> CreateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var db = await _dbFactory.CreateDbContextAsync();

        if (user.CreatedAt == default)
            user.CreatedAt = DateTime.Now;

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.UserId;
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserName == userName);
    }

    public async Task<bool> ExistsByUserNameAsync(string userName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Users.AnyAsync(x => x.UserName == userName);
    }

    public async Task<bool> UpdateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.Users
            .FirstOrDefaultAsync(x => x.UserId == user.UserId);

        if (existing is null)
            return false;

        var createdAt = existing.CreatedAt;

        db.Entry(existing).CurrentValues.SetValues(user);
        existing.CreatedAt = createdAt;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task DeleteAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var user = await db.Users
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (user is null)
            return;

        db.Users.Remove(user);
        await db.SaveChangesAsync();
    }
}