using lc.Models.Enums;

namespace lc.Models;

public class Book
{
    public int BookId { get; set; }

    public int PublisherId { get; set; }
    public User Publisher { get; set; } = null!;

    public string Title { get; set; } = "Без названия";
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
            BookId = BookId,
            PublisherId = PublisherId,
            Publisher = Publisher,
            Title = Title,
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
            Categories = [.. Categories],
            Chapters = [.. Chapters],
            Comments = [.. Comments],
            BookViews = [.. BookViews],
            BookRatings = [.. BookRatings]
        };
    }
}