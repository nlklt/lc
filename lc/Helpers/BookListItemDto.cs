using lc.Models;
using lc.Models.Enums;

namespace lc.Helpers
{
    public class BookListItemDto
    {
        public int BookId { get; set; }
        public string Title { get; set; } = "Без названия";

        public int PublisherId { get; set; } = new();
        public User? Publisher { get; set; } = new();

        public string? AuthorName { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string? CoverImagePath { get; set; } = string.Empty;

        public BookStatus BookStatus { get; set; }
        public WritingStatus WritingStatus { get; set; }
        public Language Language { get; set; }

        public int AgeRating { get; set; }

        public long SymbolsCount { get; set; }
        public int ChaptersCount { get; set; }

        public long Views { get; set; }
        public decimal Rating { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<Tag> Tags { get; set; } = [];
        public ICollection<Category> Categories { get; set; } = [];

        public BookListItemDto Clone()
        {
            return new BookListItemDto
            {
                BookId = BookId,
                Title = Title,
                PublisherId = PublisherId,
                Publisher = Publisher,
                AuthorName = AuthorName,
                Description = Description,
                CoverImagePath = CoverImagePath,
                BookStatus = BookStatus,
                WritingStatus = WritingStatus,
                Language = Language,
                AgeRating = AgeRating,
                SymbolsCount = SymbolsCount,
                ChaptersCount = ChaptersCount,
                Views = Views,
                Rating = Rating,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                Tags = [.. Tags],
                Categories = [.. Categories]
            };
        }
    }
}
