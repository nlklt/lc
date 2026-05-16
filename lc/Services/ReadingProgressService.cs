using lc.Data.Repositories.Interfaces;
using lc.Models;
using lc.Services.Interfaces;

namespace lc.Services;

public sealed class ReadingProgressService : IReadingProgressService
{
    private readonly IReadingProgressRepository _repository;

    public ReadingProgressService(IReadingProgressRepository repository)
    {
        _repository = repository;
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

        await _repository.AddOrUpdateAsync(progress);
    }

    public async Task<ReadingProgress?> GetLastBookProgressAsync(int userId, int bookId)
    {
        var progresses = await _repository.GetByUserIdAsync(userId);

        return progresses
            .Where(x => x.BookId == bookId)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefault();
    }
}