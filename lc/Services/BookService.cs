using lc.Data.Repositories.Interfaces;
using lc.Infrastructure.Data;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace lc.Services
{
    public sealed class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IChapterRepository _chapterRepository;
        private readonly ITagRepository _tagRepository;
        private readonly ICategoryRepository _categoryRepository;

        public BookService(
            IBookRepository bookRepository,
            IChapterRepository chapterRepository,
            ITagRepository tagRepository,
            ICategoryRepository categoryRepository)
        {
            _bookRepository = bookRepository;
            _chapterRepository = chapterRepository;
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _categoryRepository = categoryRepository;
        }
        public Task<Book?> GetBookByIdAsync(int bookId) 
            => _bookRepository.GetByIdAsync(bookId);

        public async Task<int> CreateBookAsync(Book book)
        {
            if (book is null)
                throw new ArgumentNullException(nameof(book));

            NormalizeDraft(book);

            book.BookStatus = BookStatus.Draft;
            book.CreatedAt = DateTime.Now;
            book.UpdatedAt = DateTime.Now;

            return await _bookRepository.CreateAsync(book);
        }

        public async Task UpdateBookAsync(Book book)
        {
            if (book is null)
                throw new ArgumentNullException(nameof(book));

            var existing = await _bookRepository.GetByIdAsync(book.BookId, includeChapters: false);
            if (existing is null)
                throw new InvalidOperationException($"Книга с ChapterId={book.BookId} не найдена.");

            NormalizeDraft(book);

            book.CreatedAt = existing.CreatedAt;
            book.UpdatedAt = DateTime.Now;
            book.BookStatus = BookStatus.Draft;

            await _bookRepository.UpdateAsync(book);
        }

        public Task DeleteBookAsync(int bookId)
        {
            return _bookRepository.DeleteAsync(bookId);
        }

        public async Task<int> SaveChapterAsync(int bookId, Chapter chapter)
        {
            if (chapter is null)
                throw new ArgumentNullException(nameof(chapter));

            var book = await _bookRepository.GetByIdAsync(bookId, includeChapters: true);
            if (book is null)
                throw new InvalidOperationException($"Книга с ChapterId={bookId} не найдена.");

            if (book.BookStatus != BookStatus.Draft)
                throw new InvalidOperationException("Главы можно менять только у черновика.");

            chapter.BookId = bookId;

            if (chapter.ChapterNumber <= 0)
            {
                chapter.ChapterNumber = (book.Chapters?.Count ?? 0) + 1;
            }

            if (chapter.ChapterId == 0)
            {
                return await _chapterRepository.CreateAsync(chapter);
            }

            await _chapterRepository.UpdateAsync(chapter);
            return chapter.ChapterId;
        }

        public async Task PublishBookAsync(int bookId)
        {
            var book = await _bookRepository.GetByIdAsync(bookId, includeChapters: true);
            if (book is null)
                throw new InvalidOperationException($"Книга с ChapterId={bookId} не найдена.");

            if (book.BookStatus == BookStatus.Published)
                return;

            var chapters = (book.Chapters ?? new List<Chapter>())
                .OrderBy(c => c.ChapterNumber)
                .ToList();

            ValidateForPublish(book, chapters);

            book.BookStatus = BookStatus.Published;
            book.UpdatedAt = DateTime.Now;

            await _bookRepository.UpdateAsync(book);
        }

        public async Task SaveBookAsync(int bookId)
        {
            var book = await _bookRepository.GetByIdAsync(bookId, includeChapters: true);
            if (book is null)
                throw new InvalidOperationException($"Книга с ChapterId={bookId} не найдена.");

            if (book.BookStatus == BookStatus.Draft)
                return;

            var chapters = (book.Chapters ?? new List<Chapter>())
                .OrderBy(c => c.ChapterNumber)
                .ToList();

            ValidateForPublish(book, chapters);

            book.BookStatus = BookStatus.Published;
            book.UpdatedAt = DateTime.Now;

            await _bookRepository.UpdateAsync(book);
        }

        public async Task<ReaderSession?> OpenReaderAsync(int bookId, int? chapterNumber = null)
        {
            var book = await _bookRepository.GetByIdAsync(bookId, includeChapters: true);
            if (book is null)
                return null;

            if (book.BookStatus != BookStatus.Published)
                throw new InvalidOperationException("В ридер можно открывать только опубликованную книгу.");

            var chapters = (book.Chapters ?? new List<Chapter>())
                .OrderBy(c => c.ChapterNumber)
                .ToList();

            if (chapters.Count == 0)
                throw new InvalidOperationException("У книги нет глав.");

            Chapter? current;

            if (chapterNumber.HasValue)
            {
                current = chapters.FirstOrDefault(c => c.ChapterNumber == chapterNumber.Value);
                if (current is null)
                    throw new InvalidOperationException($"Глава №{chapterNumber.Value} не найдена.");
            }
            else
            {
                current = chapters[0];
            }

            var index = chapters.FindIndex(c => c.ChapterId == current.ChapterId);

            return new ReaderSession
            {
                Book = book,
                Chapters = chapters,
                CurrentChapter = current,
                PreviousChapter = index > 0 ? chapters[index - 1] : null,
                NextChapter = index >= 0 && index < chapters.Count - 1 ? chapters[index + 1] : null
            };
        }

        private static void NormalizeDraft(Book book)
        {
            book.Title = NormalizeString(book.Title);
            book.AuthorName = NormalizeString(book.AuthorName);
            book.Description = NormalizeString(book.Description);
            book.CoverImagePath = NormalizeString(book.CoverImagePath);

            if (book.Views < 0)
                book.Views = 0;

            if (book.Rating < 0)
                book.Rating = 0;
        }

        private static string? NormalizeString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }

        private static void ValidateForPublish(Book book, IReadOnlyList<Chapter> chapters)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(book.Title))
                errors.Add("Название книги");
            
            if (string.IsNullOrWhiteSpace(book.AuthorName))
                errors.Add("Автор");

            if (string.IsNullOrWhiteSpace(book.Description))
                errors.Add("Описание");

            if (string.IsNullOrWhiteSpace(book.CoverImagePath))
                errors.Add("Обложка");

            if (book.Language == default)
                errors.Add("Язык");

            if (book.AgeRating <= 0)
                errors.Add("Возрастной рейтинг");

            if (chapters.Count == 0)
                errors.Add("Хотя бы одна глава");

            foreach (var chapter in chapters)
            {
                if (string.IsNullOrWhiteSpace(chapter.Title))
                    errors.Add($"Заголовок главы №{chapter.ChapterNumber}");

                if (string.IsNullOrWhiteSpace(chapter.Text))
                    errors.Add($"Текст главы №{chapter.ChapterNumber}");
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Нельзя опубликовать книгу. Не заполнены поля: " + string.Join(", ", errors));
            }
        }

        public Task<IReadOnlyList<BookListItem>> GetCatalogAsync(BookFilterCriteria criteria)
           => _bookRepository.SearchAsync(criteria);
        public async Task<IReadOnlyList<Category>> GetAllCategoriesAsync() 
            => (IReadOnlyList<Category>)await _categoryRepository.GetAllAsync();
        public async Task<IReadOnlyList<Tag>> GetAllTagsAsync() 
            => (IReadOnlyList<Tag>)await _tagRepository.GetAllAsync();
    }
}