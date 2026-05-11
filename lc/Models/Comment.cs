using System.Windows;

namespace lc.Models
{
    public class Comment
    {
        public int CommentId { get; set; }
        public int UserId {  get; set; }
        public int BookId { get; set; }
        public int? ChapterId { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        public User? User { get; set; }
    }
}
