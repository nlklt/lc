using lc.Models;

namespace lc.Helpers
{
    public sealed class ReaderSession
    {
        public required Book Book { get; init; }
        public IReadOnlyList<Chapter> Chapters { get; init; } = [];

        public Chapter? CurrentChapter { get; init; }
        public Chapter? PreviousChapter { get; init; }
        public Chapter? NextChapter { get; init; }

        public IReadOnlyList<Comment> ChapterComments { get; init; } = [];
    }
}