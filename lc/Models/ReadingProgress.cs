namespace lc.Models;

public class ReadingProgress
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public int ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;

    public int ProgressPercent { get; set; }
    public int LastPosition { get; set; }
    public DateTime UpdatedAt { get; set; }
}