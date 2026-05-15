namespace lc.Models
{
    public class Tag
    {
        public int TagId { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<Book> Books { get; set; } = [];
    }
}
