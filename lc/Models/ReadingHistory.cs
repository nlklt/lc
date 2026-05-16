namespace lc.Models;

public class ReadingHistory
{
    public int HistoryId { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public DateTime LastOpenedAt { get; set; }
}