using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using lc.Services.Interfaces;

namespace lc.Services;

public sealed class AuthorRequestService : IAuthorRequestService
{
    private readonly IAuthorRequestRepository _requestRepository;
    private readonly IUserRepository _userRepository;

    public AuthorRequestService(
        IAuthorRequestRepository requestRepository,
        IUserRepository userRepository)
    {
        _requestRepository = requestRepository;
        _userRepository = userRepository;
    }

    public async Task<int> SendRequestAsync(int userId, string message)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("Пользователь не найден.");

        if (user.Role is UserRole.Writer or UserRole.Admin)
            throw new InvalidOperationException("Пользователь уже является автором.");

        if (await _requestRepository.HasPendingRequestAsync(userId))
            throw new InvalidOperationException("У вас уже есть активная заявка.");

        var normalizedMessage = string.IsNullOrWhiteSpace(message)
            ? throw new InvalidOperationException("Введите причину или комментарий к заявке.")
            : message.Trim();

        if (normalizedMessage.Length > 2000)
            throw new InvalidOperationException("Слишком длинное сообщение.");

        var request = new AuthorRequest
        {
            UserId = userId,
            Message = normalizedMessage,
            Status = AuthorRequestStatus.Pending,
            CreatedAt = DateTime.Now
        };

        return await _requestRepository.CreateAsync(request);
    }

    public async Task CancelPendingRequestAsync(int userId)
    {
        var request = await _requestRepository.GetPendingByUserIdAsync(userId);
        if (request is null)
            return;

        request.Status = AuthorRequestStatus.Rejected;
        request.ReviewerId = null;
        request.ReviewComment = "Отменено пользователем";
        request.ReviewedAt = DateTime.Now;

        await _requestRepository.UpdateAsync(request);
    }

    public Task<IReadOnlyList<AuthorRequest>> GetMyRequestsAsync(int userId)
        => _requestRepository.GetByUserIdAsync(userId);

    public Task<IReadOnlyList<AuthorRequest>> GetPendingRequestsAsync()
        => _requestRepository.GetPendingRequestsAsync();

    public Task ApproveAsync(int requestId, int reviewerId, string? reviewComment = null)
        => _requestRepository.ApproveAsync(requestId, reviewerId, reviewComment);

    public Task RejectAsync(int requestId, int reviewerId, string? reviewComment = null)
        => _requestRepository.RejectAsync(requestId, reviewerId, reviewComment);

    public Task<bool> HasPendingRequestAsync(int userId)
        => _requestRepository.HasPendingRequestAsync(userId);
}