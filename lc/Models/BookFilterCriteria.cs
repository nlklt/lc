using lc.Models.Enums;

namespace lc.Models
{
    public sealed class BookFilterCriteria
    {
        public string? SearchText { get; set; }

        public int? PublisherId { get; set; }

        public List<BookStatus> BookStatuses { get; set; } = [];
        public List<WritingStatus> WritingStatuses { get; set; } = [];

        public List<int> IncludeTagIds { get; set; } = [];
        public List<int> ExcludeTagIds { get; set; } = [];

        public List<int> IncludeCategoryIds { get; set; } = [];
        public List<int> ExcludeCategoryIds { get; set; } = [];

        public bool StrictTagMatch { get; set; }
        public bool StrictCategoryMatch { get; set; }

        public Language? Language { get; set; }
        public int? AgeRating { get; set; }

        public double? RatingFrom { get; set; }
        public double? RatingTo { get; set; }

        public int? ChaptersFrom { get; set; }
        public int? ChaptersTo { get; set; }

        public int? SymbolsFrom { get; set; }
        public int? SymbolsTo { get; set; }

        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }

        public string SortField { get; set; } = nameof(BookListItem.Title);
        public bool SortAscending { get; set; } = true;
    }
}
