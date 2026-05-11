namespace lc.Models
{
    public class ReadingProgress
    {
        public int UserId { get; set; }
        public int BookId { get; set; }
        public int ChapterId { get; set; }

        public int ProgressPercent { get; set; }
        public int LastPosition { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}