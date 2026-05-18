using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure.Repositories.Sql;

public sealed class AuthorRequestRepository : IAuthorRequestRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AuthorRequestRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<AuthorRequest?> GetByIdAsync(int requestId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.AuthorRequests
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Reviewer)
            .FirstOrDefaultAsync(x => x.RequestId == requestId);
    }

    public async Task<AuthorRequest?> GetPendingByUserIdAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.AuthorRequests
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Status == AuthorRequestStatus.Pending);
    }

    public async Task<IReadOnlyList<AuthorRequest>> GetPendingAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.AuthorRequests
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.Status == AuthorRequestStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuthorRequest>> GetByUserIdAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.AuthorRequests
            .AsNoTracking()
            .Include(x => x.Reviewer)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuthorRequest>> GetPendingRequestsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.AuthorRequests
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

        await using var db = await _dbFactory.CreateDbContextAsync();

        db.AuthorRequests.Add(request);
        await db.SaveChangesAsync();

        return request.RequestId;
    }

    public async Task UpdateAsync(AuthorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.AuthorRequests
            .FirstOrDefaultAsync(x => x.RequestId == request.RequestId)
            ?? throw new InvalidOperationException(
                $"Заявка с RequestId={request.RequestId} не найдена.");

        db.Entry(existing).CurrentValues.SetValues(request);

        await db.SaveChangesAsync();
    }

    public async Task ApproveAsync(int requestId, int reviewerId, string? reviewComment = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var request = await db.AuthorRequests
            .FirstOrDefaultAsync(x => x.RequestId == requestId)
            ?? throw new InvalidOperationException(
                $"Заявка с RequestId={requestId} не найдена.");

        if (request.Status != AuthorRequestStatus.Pending)
            throw new InvalidOperationException("Заявка уже обработана.");

        var user = await db.Users
            .FirstOrDefaultAsync(x => x.UserId == request.UserId)
            ?? throw new InvalidOperationException("Пользователь не найден.");

        request.Status = AuthorRequestStatus.Approved;
        request.ReviewerId = reviewerId;
        request.ReviewComment = string.IsNullOrWhiteSpace(reviewComment)
            ? null
            : reviewComment.Trim();

        request.ReviewedAt = DateTime.Now;

        user.Role = UserRole.Writer;

        await db.SaveChangesAsync();
    }

    public async Task RejectAsync(int requestId, int reviewerId, string? reviewComment = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var request = await db.AuthorRequests
            .FirstOrDefaultAsync(x => x.RequestId == requestId)
            ?? throw new InvalidOperationException(
                $"Заявка с RequestId={requestId} не найдена.");

        if (request.Status != AuthorRequestStatus.Pending)
            throw new InvalidOperationException("Заявка уже обработана.");

        request.Status = AuthorRequestStatus.Rejected;
        request.ReviewerId = reviewerId;
        request.ReviewComment = string.IsNullOrWhiteSpace(reviewComment)
            ? null
            : reviewComment.Trim();

        request.ReviewedAt = DateTime.Now;

        await db.SaveChangesAsync();
    }

    public async Task<bool> HasPendingRequestAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.AuthorRequests.AnyAsync(x =>
            x.UserId == userId &&
            x.Status == AuthorRequestStatus.Pending);
    }
}