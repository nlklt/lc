namespace lc.Models;

public class UserLibraryListBook
{
    public int ListId { get; set; }
    public UserLibraryList? List { get; set; }

    public int BookId { get; set; }
    public Book? Book { get; set; }

    public int UserId { get; set; }

    public DateTime AddedAt { get; set; }
}