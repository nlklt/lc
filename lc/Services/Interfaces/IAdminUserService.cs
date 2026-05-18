using lc.Helpers;

namespace lc.Services.Interfaces;

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserRowDto>> GetUsersAsync();
    Task ToggleCommentBlockAsync(int userId);
    Task DeleteUserAsync(int userId);
}