namespace lc.Models;

public class BookRating
{
    public int BookId { get; set; }
    public Book? Book { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public byte Rating { get; set; }
    public DateTime RatedAt { get; set; }
}