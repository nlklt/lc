using lc.Models.Enums;

namespace lc.Helpers;

public sealed class MyBookCardDto
{
    public int BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? CoverImagePath { get; init; }
    public BookStatus BookStatus { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int ChaptersCount { get; init; }

    public bool IsPublished => BookStatus == BookStatus.Published;

    public string StatusText => BookStatus switch
    {
        BookStatus.Draft => "Черновик",
        BookStatus.Published => "Опубликована",
        BookStatus.Archived => "В архиве",
        _ => BookStatus.ToString()
    };
}