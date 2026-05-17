namespace lc.Helpers;

public sealed class ContinueReadingItemDto
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CoverPath { get; set; }
    public int? LastChapterNumber { get; set; }
    public int ReadingProgressPercent { get; set; }
    public DateTime LastOpenedAt { get; set; }

    public string LastChapterText =>
        LastChapterNumber.HasValue
            ? $"Глава {LastChapterNumber.Value}"
            : "Продолжить с начала";
}