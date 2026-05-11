using System.Net;
using static System.Net.Mime.MediaTypeNames;

namespace lc.Models
{
    public class Chapter
    {
        public int ChapterId { get; set; }
        public int BookId { get; set; }
        public int ChapterNumber { get; set; }

        public string? Title { get; set; }
        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
