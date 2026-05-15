namespace lc.Services.Interfaces;

public interface IBookStatsService
{
    Task RegisterViewAsync(int bookId);
    Task SetRatingAsync(int bookId, byte rating);
    Task RecalculateBookCacheAsync(int bookId);
}