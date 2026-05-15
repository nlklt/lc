using lc.Models;

namespace lc.Services.Interfaces
{
    public interface IReaderService
    {
        Task<ReaderSession?> OpenAsync(int bookId, int? chapterNumber = null);
    }
}