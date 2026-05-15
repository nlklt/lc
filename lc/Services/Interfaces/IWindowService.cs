namespace lc.Services.Interfaces
{
    public interface IWindowService
    {
        Task OpenReaderAsync(int bookId, int? chapterNumber = null); // !!! не chapterId
    }
}