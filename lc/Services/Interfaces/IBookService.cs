using lc.Helpers;
using lc.Models;
using lc.Models.Enums;

namespace lc.Services
{
    public interface IBookService
    {
        Task<Book?> GetBookByIdAsync(int bookId);

        Task<int> CreateBookAsync(Book book);
        Task UpdateBookAsync(Book book);

        Task ArchiveBookAsync(int bookId);
        Task RestoreBookAsync(int bookId);
        Task DeleteBookAsync(int bookId);

        Task PublishBookAsync(int bookId);

        Task<IReadOnlyList<BookListItemDto>> GetCatalogAsync(BookFilterCriteria criteria);
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
    }
}