using lc.Data.Repositories.Interfaces;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;

namespace lc.Services
{
    public sealed class ReaderService : IReaderService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IChapterRepository _chapterRepository;
        private readonly IUserRepository _userRepository;

        public ReaderService(
            IBookRepository bookRepository,
            IChapterRepository chapterRepository,
            IUserRepository userRepository)
        {
            _bookRepository = bookRepository;
            _chapterRepository = chapterRepository;
            _userRepository = userRepository;
        }

        public async Task<ReaderSession?> OpenAsync(int bookId, int? chapterNumber = null)
        {
            var book = await _bookRepository.GetByIdAsync(bookId, includeChapters: true);
            if (book is null)
                return null;

            if (book.BookStatus != BookStatus.Published)
                throw new InvalidOperationException("В ридер можно открыть только опубликованную книгу.");

            var chapters = (book.Chapters ?? new List<Chapter>())
                .OrderBy(c => c.ChapterNumber)
                .ToList();

            if (chapters.Count == 0)
                throw new InvalidOperationException("У книги нет глав.");

            Chapter currentChapter;

            if (chapterNumber.HasValue)
            {
                currentChapter = chapters.FirstOrDefault(c => c.ChapterNumber == chapterNumber.Value)
                    ?? throw new InvalidOperationException($"Глава №{chapterNumber.Value} не найдена.");
            }
            else
            {
                currentChapter = chapters[0];
            }

            var index = chapters.FindIndex(c => c.ChapterId == currentChapter.ChapterId);

            return new ReaderSession
            {
                Book = book,
                Chapters = chapters,
                CurrentChapter = currentChapter,
                PreviousChapter = index > 0 ? chapters[index - 1] : null,
                NextChapter = index >= 0 && index < chapters.Count - 1 ? chapters[index + 1] : null
            };
        }

        private async Task EnsureBookExistsAndPublishedAsync(int bookId)
        {
            var book = await _bookRepository.GetByIdAsync(bookId, includeChapters: false)
                ?? throw new InvalidOperationException($"Книга с ChapterId={bookId} не найдена.");

            if (book.BookStatus != BookStatus.Published)
                throw new InvalidOperationException("Комментарий можно оставлять только к опубликованной книге.");
        }
    }
}