using lc.Models;

namespace lc.Services
{
    public interface IReaderService
    {
        Task<ReaderSession?> OpenAsync(int bookId, int? chapterNumber = null);

        Task<int> AddChapterCommentAsync(int bookId, int chapterId, int userId, string text);

        Task<IReadOnlyList<Comment>> GetChapterCommentsAsync(int chapterId);
    }
}