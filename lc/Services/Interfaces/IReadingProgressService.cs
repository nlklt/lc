using lc.Models;

namespace lc.Services.Interfaces;

public interface IReadingProgressService
{
    Task SaveProgressAsync(
        int userId,
        int bookId,
        int chapterId,
        int progressPercent,
        int lastPosition);

    Task<ReadingProgress?> GetLastBookProgressAsync(int userId, int bookId);
    Task MarkBookOpenedAsync(int userId, int bookId);
}