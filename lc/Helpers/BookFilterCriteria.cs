using lc.Models.Enums;

namespace lc.Helpers
{
    public sealed class BookFilterCriteria
    {
        public string? SearchText { get; set; }

        public bool StrictTagMatch { get; set; }
        public bool StrictCategoryMatch { get; set; }

        public List<int> IncludeCategoryIds { get; set; } = [];
        public List<int> ExcludeCategoryIds { get; set; } = [];

        public List<int> IncludeTagIds { get; set; } = [];
        public List<int> ExcludeTagIds { get; set; } = [];

        public List<BookStatus> IncludeBookStatuses { get; set; } = [];
        public List<BookStatus> ExcludeBookStatuses { get; set; } = [];

        public List<WritingStatus> IncludeWritingStatuses { get; set; } = [];
        public List<WritingStatus> ExcludeWritingStatuses { get; set; } = [];

        public List<Language> IncludeLanguages { get; set; } = [];
        public List<Language> ExcludeLanguages { get; set; } = [];

        public List<int> IncludeAgeRatings { get; set; } = [];
        public List<int> ExcludeAgeRatings { get; set; } = [];

        public decimal? RatingFrom { get; set; }
        public decimal? RatingTo { get; set; }

        public int? ChaptersFrom { get; set; }
        public int? ChaptersTo { get; set; }

        public int? SymbolsFrom { get; set; }
        public int? SymbolsTo { get; set; }

        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }

        public string SortField { get; set; } = nameof(BookListItemDto.Title);
        public bool SortAscending { get; set; } = true;
    }
}
