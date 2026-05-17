using lc.Models;

namespace lc.Services.Interfaces;

public interface IAuthorRequestService
{
    Task<int> SendRequestAsync(int userId, string message);
    Task CancelPendingRequestAsync(int userId);

    Task<IReadOnlyList<AuthorRequest>> GetMyRequestsAsync(int userId);
    Task<IReadOnlyList<AuthorRequest>> GetPendingRequestsAsync();

    Task ApproveAsync(int requestId, int reviewerId, string? reviewComment = null);
    Task RejectAsync(int requestId, int reviewerId, string? reviewComment = null);

    Task<bool> HasPendingRequestAsync(int userId);
}