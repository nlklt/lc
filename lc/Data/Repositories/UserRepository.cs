using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using Microsoft.EntityFrameworkCore;

namespace lc.Data.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.CreatedAt == default)
            user.CreatedAt = DateTime.Now;

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return user.UserId;
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserName == userName);
    }

    public async Task<bool> ExistsByUserNameAsync(string userName)
    {
        return await _db.Users.AnyAsync(x => x.UserName == userName);
    }

    public async Task<bool> UpdateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var existing = await _db.Users
            .FirstOrDefaultAsync(x => x.UserId == user.UserId);

        if (existing is null)
            return false;

        var createdAt = existing.CreatedAt;

        _db.Entry(existing).CurrentValues.SetValues(user);
        existing.CreatedAt = createdAt;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task DeleteAsync(int userId)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (user is null)
            return;

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }
}