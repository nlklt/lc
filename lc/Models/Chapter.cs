namespace lc.Models
{
    public class Chapter
    {
        public int ChapterId { get; set; }
        public int BookId { get; set; }
        public Book? Book { get; set; }

        public int ChapterNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
