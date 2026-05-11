//// ФИЛЬТРАЦИЯ
//using lc.Models;
//using lc.Models.Enums;
//using lc.ViewModels.Base;
//using System.CodeDom.Compiler;
//using System.Collections.ObjectModel;
//using System.Windows.Media;

//private bool _isCategoriesOpen;
//private bool _isTagsOpen;
//private bool _isStrictCategoryMatch;
//private bool _isStrictTagMatch;

//public Range<double> Rating { get; } = new();
//public Range<int> Chapters { get; } = new();
//public Range<int> Symbols { get; } = new();
//public Range<DateTime> CreatedAt { get; } = new();
//public ObservableCollection<TriStateOption<Category>> CategoryOptions { get; set; } = [];
//public ObservableCollection<TriStateOption<Tag>> TagOptions { get; set; } = [];
//public ObservableCollection<TriStateOption<BookStatus>> StatusOptions { get; }
//public ObservableCollection<TriStateOption<Language>> LanguageOptions { get; }
//public ObservableCollection<TriStateOption<int>> AgeRatingOptions { get; }

//public class Range<T> : ViewModelBase where T : struct, IComparable<T>
//{
//    private T? _from;
//    public T? From
//    {
//        get => _from;
//        set => SetProperty(ref _from, value);
//    }

//    private T? _to;
//    public T? To
//    {
//        get => _to;
//        set => SetProperty(ref _to, value);
//    }
//}
//public class TriStateOption<T> : ViewModelBase
//{
//    public T Value { get; }
//    public string Name { get; }

//    private CheckBoxState _state = CheckBoxState.Neutral;
//    public CheckBoxState State
//    {
//        get => _state;
//        set => SetProperty(ref _state, value);
//    }

//    public TriStateOption(T value, string name)
//    {
//        Value = value;
//        Name = name;
//    }
//}
//private void CheckBoxRefresh<T>(ObservableCollection<TriStateOption<T>> options)
//{
//    foreach (var opt in options)
//        opt.PropertyChanged += (_, e) =>
//        {
//            if (e.PropertyName == nameof(TriStateOption<T>.State))
//            {
//                OnPropertyChanged(nameof(CategoriesSummary));
//                OnPropertyChanged(nameof(TagsSummary));
//                OnPropertyChanged(nameof(CategorySelectedCount));
//                OnPropertyChanged(nameof(TagSelectedCount));
//                OnPropertyChanged(nameof(TagExcludedCount));
//                BooksView.Refresh();
//            }
//        };
//}
//private void RangeRefresh<T>(Range<T> opt) where T : struct, IComparable<T>
//{
//    opt.PropertyChanged += (_, e) =>
//    {
//        if (e.PropertyName == nameof(Range<T>.From) ||
//            e.PropertyName == nameof(Range<T>.To))
//        {
//            OnPropertyChanged(nameof(Range<T>.From));
//            OnPropertyChanged(nameof(Range<T>.To));
//            BooksView.Refresh();
//        }
//    };
//}

//public bool IsCategoriesOpen
//{
//    get => _isCategoriesOpen;
//    set => SetProperty(ref _isCategoriesOpen, value);
//}
//public bool IsTagsOpen
//{
//    get => _isTagsOpen;
//    set => SetProperty(ref _isTagsOpen, value);
//}
//public bool IsStrictCategoryMatch
//{
//    get => _isStrictCategoryMatch;
//    set
//    {
//        if (SetProperty(ref _isStrictCategoryMatch, value))
//            BooksView.Refresh();
//    }
//}
//public bool IsStrictTagMatch
//{
//    get => _isStrictTagMatch;
//    set
//    {
//        if (SetProperty(ref _isStrictTagMatch, value))
//            BooksView.Refresh();
//    }
//}
//public int CategorySelectedCount => CategoryOptions.Count(c => c.State == CheckBoxState.Exclude);
//public int CategoryExcludedCount => CategoryOptions.Count(c => c.State == CheckBoxState.Include);
//public int TagSelectedCount => TagOptions.Count(c => c.State == CheckBoxState.Exclude);
//public int TagExcludedCount => TagOptions.Count(c => c.State == CheckBoxState.Include);
//public string CategoriesSummary =>
//    CategorySelectedCount == 0 && CategoryExcludedCount == 0
//        ? "Любые >"
//        : $"+{CategorySelectedCount} / -{CategoryExcludedCount}";
//public string TagsSummary =>
//    TagSelectedCount == 0 && TagSelectedCount == 0
//        ? "Любые >"
//        : $"+{TagSelectedCount} / -{TagExcludedCount}";

//private bool FilterBooks(object obj)
//{
//    if (obj is not Book book)
//        return false;

//    if (!string.IsNullOrWhiteSpace(SearchText))
//    {
//        var text = SearchText.Trim().ToLower();

//        bool textMatch =
//            (book.Title?.ToLower().Contains(text) ?? false) ||
//            (book.AuthorName?.ToLower().Contains(text) ?? false);

//        if (!textMatch)
//            return false;
//    }

//    if (!MatchCategories(book))
//        return false;

//    if (!MatchTags(book))
//        return false;

//    if (!MatchTriState(book.BookStatus, StatusOptions))
//        return false;

//    if (!MatchTriState(book.Language, LanguageOptions))
//        return false;

//    if (!MatchTriState(book.AgeRating, AgeRatingOptions))
//        return false;

//    if (!MatchRange(book.Rating, Rating.From, Rating.To))
//        return false;

//    if (!MatchRange(book.ChaptersCount, Chapters.From, Chapters.To))
//        return false;

//    if (!MatchRange(book.SymbolsCount, Symbols.From, Symbols.To))
//        return false;

//    if (!MatchRange(book.CreatedAt, CreatedAt.From, CreatedAt.To))
//        return false;

//    return true;
//}
//private bool MatchCategories(Book book)
//{
//    var bookCategories = book.Categories ?? new List<Category>();

//    var include = CategoryOptions
//        .Where(c => c.State == CheckBoxState.Exclude)
//        .Select(c => c.Value)
//        .ToList();

//    var exclude = CategoryOptions
//        .Where(c => c.State == CheckBoxState.Include)
//        .Select(c => c.Value)
//        .ToList();

//    if (include.Count == 0 && exclude.Count == 0)
//        return true;

//    if (exclude.Any(bookCategories.Contains))
//        return false;

//    if (include.Count == 0)
//        return true;

//    if (IsStrictCategoryMatch)
//        return include.All(bookCategories.Contains);

//    return include.Any(bookCategories.Contains);
//}
//private bool MatchTags(Book book)
//{
//    var bookTags = book.Tags ?? new List<Tag>();

//    var include = TagOptions
//        .Where(c => c.State == CheckBoxState.Exclude)
//        .Select(c => c.Value)
//        .ToList();

//    var exclude = TagOptions
//        .Where(c => c.State == CheckBoxState.Include)
//        .Select(c => c.Value)
//        .ToList();

//    if (include.Count == 0 && exclude.Count == 0)
//        return true;

//    if (exclude.Any(bookTags.Contains))
//        return false;

//    if (include.Count == 0)
//        return true;

//    if (IsStrictTagMatch)
//        return include.All(bookTags.Contains);

//    return include.Any(bookTags.Contains);
//}
//private static bool MatchTriState<T>(T value, IEnumerable<TriStateOption<T>> options)
//{
//    var include = options
//        .Where(o => o.State == CheckBoxState.Exclude)
//        .Select(o => o.Value)
//        .ToList();

//    var exclude = options
//        .Where(o => o.State == CheckBoxState.Include)
//        .Select(o => o.Value)
//        .ToList();

//    if (include.Count == 0 && exclude.Count == 0)
//        return true;

//    if (exclude.Contains(value))
//        return false;

//    if (include.Count == 0)
//        return true;

//    return include.Contains(value);
//}
//private static bool MatchRange<T>(T value, T? from, T? to) where T : struct, IComparable<T>
//{
//    if (from.HasValue && value.CompareTo(from.Value) < 0)
//        return false;

//    if (to.HasValue && value.CompareTo(to.Value) > 0)
//        return false;

//    return true;
//}

//// Коллекции для значений фильтров
//using lc.Models;
//using lc.Models.Enums;
//using System.CodeDom.Compiler;
//using System.Collections.ObjectModel;
//using System.Windows.Media;

//{
//    CategoryOptions = new ObservableCollection<TriStateOption<Category>>(
//        Enum.GetValues<Category>().Select(x => new TriStateOption<Category>(x, x.ToString())));

//    TagOptions = new ObservableCollection<TriStateOption<Tag>>(
//        Enum.GetValues<Tag>().Select(x => new TriStateOption<Tag>(x, x.ToString())));

//    StatusOptions = new ObservableCollection<TriStateOption<BookStatus>>(
//        Enum.GetValues<BookStatus>().Select(x => new TriStateOption<BookStatus>(x, x.ToString())));

//    LanguageOptions = new ObservableCollection<TriStateOption<Language>>(
//        Enum.GetValues<Language>().Select(x => new TriStateOption<Language>(x, x.ToString())));

//    AgeRatingOptions = new ObservableCollection<TriStateOption<int>>
//                {
//                    new(3, "3+"),
//                    new(6, "6+"),
//                    new(12, "12+"),
//                    new(16, "16+"),
//                    new(18, "18+")
//                };
//}

//RangeRefresh(Rating);
//RangeRefresh(Chapters);
//RangeRefresh(Symbols);
//RangeRefresh(CreatedAt);

//CheckBoxRefresh(CategoryOptions);
//CheckBoxRefresh(TagOptions);
//CheckBoxRefresh(StatusOptions);
//CheckBoxRefresh(LanguageOptions);
//CheckBoxRefresh(AgeRatingOptions);