using lc.Models;

namespace lc.Services
{
    public interface IBookService
    {
        Task<IReadOnlyList<BookListItem>> GetCatalogAsync(BookFilterCriteria criteria);
        //Task<Book?> GetBookDetailsAsync(int bookId);
        Task<int> CreateDraftAsync(Book book);
        Task UpdateDraftAsync(Book book);
        Task PublishAsync(int bookId);

        Task<int> SaveChapterAsync(int bookId, Chapter chapter);
        Task<Book?> GetDraftAsync(int bookId);
        Task<ReaderSession?> OpenReaderAsync(int bookId, int? chapterNumber = null);

        Task DeleteBookAsync(int bookId);

        Task<IReadOnlyList<Category>> GetAllCategoriesAsync();
        Task<IReadOnlyList<Tag>> GetAllTagsAsync();
    }

    public sealed class ReaderSession
    {
        public required Book Book { get; init; }
        public IReadOnlyList<Chapter> Chapters { get; init; } = Array.Empty<Chapter>();
        public Chapter? CurrentChapter { get; init; }
        public Chapter? PreviousChapter { get; init; }
        public Chapter? NextChapter { get; init; }
        public IReadOnlyList<Comment> ChapterComments { get; internal set; }
    }
}