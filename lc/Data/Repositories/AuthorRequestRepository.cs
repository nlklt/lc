using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class AuthorRequestRepository : IAuthorRequestRepository
{
    private readonly AppDbContext _db;

    public AuthorRequestRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AuthorRequest?> GetByIdAsync(int requestId)
    {
        return await _db.AuthorRequests
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Reviewer)
            .FirstOrDefaultAsync(x => x.RequestId == requestId);
    }

    public async Task<AuthorRequest?> GetPendingByUserIdAsync(int userId)
    {
        return await _db.AuthorRequests
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == AuthorRequestStatus.Pending);
    }

    public async Task<IReadOnlyList<AuthorRequest>> GetPendingAsync()
    {
        return await _db.AuthorRequests
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.Status == AuthorRequestStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuthorRequest>> GetByUserIdAsync(int userId)
    {
        return await _db.AuthorRequests
            .AsNoTracking()
            .Include(x => x.Reviewer)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuthorRequest>> GetPendingRequestsAsync()
    {
        return await _db.AuthorRequests
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Reviewer)
            .Where(x => x.Status == AuthorRequestStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(AuthorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        _db.AuthorRequests.Add(request);
        await _db.SaveChangesAsync();

        return request.RequestId;
    }

    public async Task UpdateAsync(AuthorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await _db.AuthorRequests
            .FirstOrDefaultAsync(x => x.RequestId == request.RequestId)
            ?? throw new InvalidOperationException($"Заявка с RequestId={request.RequestId} не найдена.");

        _db.Entry(existing).CurrentValues.SetValues(request);
        await _db.SaveChangesAsync();
    }

    public async Task ApproveAsync(int requestId, int reviewerId, string? reviewComment = null)
    {
        var request = await _db.AuthorRequests
            .FirstOrDefaultAsync(x => x.RequestId == requestId)
            ?? throw new InvalidOperationException($"Заявка с RequestId={requestId} не найдена.");

        if (request.Status != AuthorRequestStatus.Pending)
            throw new InvalidOperationException("Заявка уже обработана.");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == request.UserId)
            ?? throw new InvalidOperationException("Пользователь не найден.");

        request.Status = AuthorRequestStatus.Approved;
        request.ReviewerId = reviewerId;
        request.ReviewComment = string.IsNullOrWhiteSpace(reviewComment) ? null : reviewComment.Trim();
        request.ReviewedAt = DateTime.Now;

        user.Role = UserRole.Writer;

        await _db.SaveChangesAsync();
    }

    public async Task RejectAsync(int requestId, int reviewerId, string? reviewComment = null)
    {
        var request = await _db.AuthorRequests
            .FirstOrDefaultAsync(x => x.RequestId == requestId)
            ?? throw new InvalidOperationException($"Заявка с RequestId={requestId} не найдена.");

        if (request.Status != AuthorRequestStatus.Pending)
            throw new InvalidOperationException("Заявка уже обработана.");

        request.Status = AuthorRequestStatus.Rejected;
        request.ReviewerId = reviewerId;
        request.ReviewComment = string.IsNullOrWhiteSpace(reviewComment) ? null : reviewComment.Trim();
        request.ReviewedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public async Task<bool> HasPendingRequestAsync(int userId)
    {
        return await _db.AuthorRequests.AnyAsync(x =>
            x.UserId == userId &&
            x.Status == AuthorRequestStatus.Pending);
    }
}