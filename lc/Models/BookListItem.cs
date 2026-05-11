using lc.Models.Enums;

namespace lc.Models
{
    public class BookListItem
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public int PublisherId { get; set; }
        public string? PublisherName { get; set; }
        public string AuthorName { get; set; }
        public string CoverImagePath { get; set; }

        public BookStatus BookStatus { get; set; }
        public WritingStatus WritingStatus { get; set; }
        public Language Language { get; set; }
        public int AgeRating { get; set; }

        public int ChaptersCount { get; set; }
        public int SymbolsCount { get; set; }

        public long Views { get; set; }
        public double Rating { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<Tag> Tags { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }
}
