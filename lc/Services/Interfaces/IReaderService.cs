using lc.Models;

namespace lc.Services
{
    public interface IReaderService
    {
        Task<ReaderSession?> OpenAsync(int bookId, int? chapterNumber = null);
    }
}