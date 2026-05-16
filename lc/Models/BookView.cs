namespace lc.Models;

public class BookView
{
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime ViewedAt { get; set; }
}