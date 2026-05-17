using lc.Data.Repositories.Interfaces;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Services.Interfaces;

namespace lc.Services;

public sealed class ReadingProgressService : IReadingProgressService
{
    private readonly IReadingProgressRepository _progressRepository;
    private readonly IReadingHistoryRepository _historyRepository;

    public ReadingProgressService(
        IReadingProgressRepository progressRepository,
        IReadingHistoryRepository historyRepository)
    {
        _progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
    }

    public async Task SaveProgressAsync(
        int userId,
        int bookId,
        int chapterId,
        int progressPercent,
        int lastPosition)
    {
        var progress = new ReadingProgress
        {
            UserId = userId,
            BookId = bookId,
            ChapterId = chapterId,
            ProgressPercent = progressPercent,
            LastPosition = lastPosition,
            UpdatedAt = DateTime.Now
        };

        await _progressRepository.AddOrUpdateAsync(progress);
    }

    public Task<ReadingProgress?> GetLastBookProgressAsync(int userId, int bookId)
        => _progressRepository.GetLastByBookAsync(userId, bookId);

    public Task MarkBookOpenedAsync(int userId, int bookId)
        => _historyRepository.AddOrUpdateAsync(userId, bookId, DateTime.Now);
}