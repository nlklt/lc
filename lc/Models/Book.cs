using lc.Models;
using lc.Models.Enums;

namespace lc.Models
{
    public class Book
    {
        public int BookId { get; set; }
        public string? Title { get; set; } = string.Empty;
        public int PublisherId { get; set; } = new();
        public User? Publisher { get; set; } = new();
        public string? AuthorName { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string? CoverImagePath { get; set; } = string.Empty;

        public List<Tag> Tags { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        public BookStatus BookStatus { get; set; }
        public WritingStatus WritingStatus { get; set; }
        public Language Language { get; set; }
        public int AgeRating { get; set; }
        
        public int SymbolsCount { get; set; }
        public int ChaptersCount { get; set; }

        public long Views { get; set; }
        public double Rating { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<Chapter> Chapters { get; set; } = [];
        public List<Comment> Comments { get; set; } = [];

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

                Tags = [.. this.Tags],
                Categories = [.. this.Categories],

                BookStatus = this.BookStatus,
                WritingStatus = this.WritingStatus,
                Language = this.Language,
                AgeRating = this.AgeRating,

                Views = this.Views,
                Rating = this.Rating,
                
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt,

                Chapters = [.. this.Chapters],
                Comments = [.. this.Comments]
            };
        }
    }
}
