using lc.Models;
using lc.Models.Enums;

namespace lc.Infrastructure.Repositories.Abstractions;

public interface IAuthorRequestRepository
{
    Task<AuthorRequest?> GetByIdAsync(int requestId);
    Task<AuthorRequest?> GetPendingByUserIdAsync(int userId);
    Task<IReadOnlyList<AuthorRequest>> GetPendingAsync();
    Task<IReadOnlyList<AuthorRequest>> GetByUserIdAsync(int userId);
    Task<IReadOnlyList<AuthorRequest>> GetPendingRequestsAsync();

    Task<int> CreateAsync(AuthorRequest request);
    Task UpdateAsync(AuthorRequest request);

    Task ApproveAsync(int requestId, int reviewerId, string? reviewComment = null);
    Task RejectAsync(int requestId, int reviewerId, string? reviewComment = null);

    Task<bool> HasPendingRequestAsync(int userId);
}