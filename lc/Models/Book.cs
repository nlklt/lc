using lc.Models.Enums;

namespace lc.Models
{
    public class Book
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
        public ICollection<Chapter> Chapters { get; set; } = [];
        public ICollection<Comment> Comments { get; set; } = [];
        public ICollection<BookView> BookViews { get; set; } = [];
        public ICollection<BookRating> BookRatings { get; set; } = [];

        public Book Clone()
        {
            return new Book
            {
                BookId = this.BookId,
                Title = this.Title,
                PublisherId = this.PublisherId,
                Publisher = this.Publisher,
                AuthorName = this.AuthorName,
                Description = this.Description,
                CoverImagePath = this.CoverImagePath,
                BookStatus = this.BookStatus,
                WritingStatus = this.WritingStatus,
                Language = this.Language,
                AgeRating = this.AgeRating,
                SymbolsCount = this.SymbolsCount,
                ChaptersCount = this.ChaptersCount,
                Views = this.Views,
                Rating = this.Rating,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt,
                Tags = [.. this.Tags],
                Categories = [.. this.Categories],
                Chapters = [.. this.Chapters],
                Comments = [.. this.Comments],
                BookViews = [.. this.BookViews],
                BookRatings = [.. this.BookRatings]
            };
        }
    }
}
