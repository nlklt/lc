namespace lc.Models;

public class Favorite
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public int BookId { get; set; }
    public Book? Book { get; set; }

    public DateTime AddedAt { get; set; }
}