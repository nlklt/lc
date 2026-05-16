namespace lc.Helpers;

public sealed class ContinueReadingItemDto
{
    public int BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? CoverPath { get; init; }
    public int? LastChapterNumber { get; init; }
    public int ReadingProgressPercent { get; init; }
    public DateTime LastOpenedAt { get; init; }

    public string LastChapterText =>
        LastChapterNumber.HasValue
            ? $"Глава {LastChapterNumber.Value}"
            : "С начала";
}