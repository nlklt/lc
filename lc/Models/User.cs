using lc.Models.Enums;

namespace lc.Models;

public class User
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string AvatarPath { get; set; } = string.Empty;
    public bool BlockedComments { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserRole Role { get; set; } = UserRole.Reader;
    public Language PreferredLanguage { get; set; } = Language.Русский;
    public string PreferredTheme { get; set; } = "Dark";

    public ICollection<Book> PublishedBooks { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<BookView> BookViews { get; set; } = [];
    public ICollection<BookRating> BookRatings { get; set; } = [];
    public ICollection<UserLibraryList> LibraryLists { get; set; } = [];
    public ICollection<ReadingHistory> ReadingHistory { get; set; } = [];
    public ICollection<ReadingProgress> ReadingProgresses { get; set; } = [];
}