using lc.Models.Enums;

namespace lc.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? AvatarPath { get; set; } = string.Empty;
        public bool BlockedComments { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserRole Role { get; set; }
        public Language PreferredLanguage { get; set; }
        public string PreferredTheme { get; set; } = "Dark";
    }
}
