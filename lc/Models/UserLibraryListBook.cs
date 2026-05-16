namespace lc.Models;

public class UserLibraryListBook
{
    public int ListId { get; set; }
    public UserLibraryList List { get; set; } = null!;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public DateTime AddedAt { get; set; }
}