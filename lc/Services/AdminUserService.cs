using lc.Helpers;
using lc.Infrastructure;
using lc.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace lc.Services;

public sealed class AdminUserService : IAdminUserService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AppState _appState;

    public AdminUserService(IDbContextFactory<AppDbContext> dbFactory, AppState appState)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
    }

    public async Task<IReadOnlyList<AdminUserRowDto>> GetUsersAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        int? currentUserId = _appState.CurrentUser?.UserId;

        return await db.Users
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminUserRowDto
            {
                UserId = x.UserId,
                UserName = x.UserName,
                Role = x.Role,
                CreatedAt = x.CreatedAt,
                BlockedComments = x.BlockedComments,
                PublishedBooksCount = x.PublishedBooks.Count,
                CommentsCount = x.Comments.Count,
                CanToggleCommentBlock = currentUserId.HasValue && x.UserId != currentUserId.Value,
                CanDelete = currentUserId.HasValue &&
                            x.UserId != currentUserId.Value &&
                            !x.PublishedBooks.Any()
            })
            .ToListAsync();
    }

    public async Task ToggleCommentBlockAsync(int userId)
    {
        var currentUserId = _appState.CurrentUser?.UserId
            ?? throw new InvalidOperationException("Действие невозможно: пользователь не авторизован.");

        if (userId == currentUserId)
            throw new InvalidOperationException("Нельзя изменять запрет комментариев для самого себя.");

        await using var db = await _dbFactory.CreateDbContextAsync();

        var user = await db.Users.FirstOrDefaultAsync(x => x.UserId == userId)
            ?? throw new InvalidOperationException("Пользователь не найден.");

        user.BlockedComments = !user.BlockedComments;
        await db.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(int userId)
    {
        var currentUserId = _appState.CurrentUser?.UserId
            ?? throw new InvalidOperationException("Действие невозможно: пользователь не авторизован.");

        if (userId == currentUserId)
            throw new InvalidOperationException("Нельзя удалить самого себя.");

        await using var db = await _dbFactory.CreateDbContextAsync();

        var user = await db.Users
            .Include(x => x.PublishedBooks)
            .FirstOrDefaultAsync(x => x.UserId == userId)
            ?? throw new InvalidOperationException("Пользователь не найден.");

        if (user.PublishedBooks.Any())
            throw new InvalidOperationException("Нельзя удалить пользователя, у которого есть книги.");

        db.Users.Remove(user);
        await db.SaveChangesAsync();
    }
}