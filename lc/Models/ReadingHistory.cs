namespace lc.Models
{
    public class ReadingHistory
    {
        public int ChapterId { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public DateTime LastOpenedAt { get; set; }
    }
}