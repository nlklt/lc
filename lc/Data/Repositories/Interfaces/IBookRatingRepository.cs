using lc.Models;

namespace lc.Data.Repositories.Interfaces
{
    public interface IBookRatingRepository
    {
        Task<BookRating?> GetAsync(int userId, int bookId);
        Task<IReadOnlyList<BookRating>> GetByBookIdAsync(int bookId);
        Task<IReadOnlyList<BookRating>> GetByUserIdAsync(int userId);

        Task AddOrUpdateAsync(int userId, int bookId, byte rating);
        Task RemoveAsync(int userId, int bookId);

        Task<decimal> GetAverageRatingAsync(int bookId);
    }
}
