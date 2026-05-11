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
        private readonly ICommentRepository _commentRepository;
        private readonly IUserRepository _userRepository;

        public ReaderService(
            IBookRepository bookRepository,
            IChapterRepository chapterRepository,
            ICommentRepository commentRepository,
            IUserRepository userRepository)
        {
            _bookRepository = bookRepository;
            _chapterRepository = chapterRepository;
            _commentRepository = commentRepository;
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

            var bookComments = await _commentRepository.GetByBookIdAsync(bookId);
            var chapterComments = await _commentRepository.GetByChapterIdAsync(currentChapter.ChapterId);

            return new ReaderSession
            {
                Book = book,
                Chapters = chapters,
                CurrentChapter = currentChapter,
                PreviousChapter = index > 0 ? chapters[index - 1] : null,
                NextChapter = index >= 0 && index < chapters.Count - 1 ? chapters[index + 1] : null,
                ChapterComments = chapterComments
            };
        }

        public async Task<int> AddChapterCommentAsync(int bookId, int chapterId, int userId, string text)
        {
            await EnsureUserCanCommentAsync(userId);
            await EnsureBookExistsAndPublishedAsync(bookId);

            var chapter = await _chapterRepository.GetByIdAsync(chapterId)
                ?? throw new InvalidOperationException($"Глава с ChapterId={chapterId} не найдена.");

            if (chapter.BookId != bookId)
                throw new InvalidOperationException("Глава не принадлежит указанной книге.");

            var comment = new Comment
            {
                UserId = userId,
                BookId = bookId,
                ChapterId = chapterId,
                Text = NormalizeCommentText(text),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            return await _commentRepository.CreateAsync(comment);
        }

        public Task<IReadOnlyList<Comment>> GetBookCommentsAsync(int bookId)
        {
            return _commentRepository.GetByBookIdAsync(bookId);
        }

        public Task<IReadOnlyList<Comment>> GetChapterCommentsAsync(int chapterId)
        {
            return _commentRepository.GetByChapterIdAsync(chapterId);
        }

        private async Task EnsureUserCanCommentAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new InvalidOperationException($"Пользователь с ChapterId={userId} не найден.");

            if (user.BlockedComments)
                throw new InvalidOperationException("Пользователю запрещено оставлять комментарии.");
        }

        private async Task EnsureBookExistsAndPublishedAsync(int bookId)
        {
            var book = await _bookRepository.GetByIdAsync(bookId, includeChapters: false)
                ?? throw new InvalidOperationException($"Книга с ChapterId={bookId} не найдена.");

            if (book.BookStatus != BookStatus.Published)
                throw new InvalidOperationException("Комментарий можно оставлять только к опубликованной книге.");
        }

        private static string NormalizeCommentText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Комментарий не может быть пустым.");

            return text.Trim();
        }
    }
}