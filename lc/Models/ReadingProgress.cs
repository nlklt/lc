namespace lc.Models;

public class ReadingProgress
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public int ChapterId { get; set; }
    public Chapter? Chapter { get; set; }

    public int ProgressPercent { get; set; }
    public int LastPosition { get; set; }
    public DateTime UpdatedAt { get; set; }
}