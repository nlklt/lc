namespace lc.Models;

public class UserLibraryList
{
    public int ListId { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<UserLibraryListBook> Books { get; set; } = [];
}