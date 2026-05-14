using lc.Data.Repositories;
using lc.Data.Repositories.Interfaces;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Infrastructure.Repositories.Sql;
using lc.Services;
using lc.Services.Interfaces;
namespace lc.Infrastructure
{
    public static class ServiceLocator
    {
        public static ITagRepository            TagRepository { get; } = new TagRepository();
        public static IUserRepository           UserRepository { get; } = new UserRepository();
        public static IChapterRepository        ChapterRepository { get; } = new ChapterRepository();
        public static ICommentRepository        CommentRepository { get; } = new CommentRepository();
        public static ICategoryRepository       CategoryRepository { get; } = new CategoryRepository();
        public static IUserLibraryRepository    UserLibraryRepository { get; } = new UserLibraryRepository();
        public static IBookRepository           BookRepository { get; } = new BookRepository(ChapterRepository, CommentRepository, TagRepository, CategoryRepository);

        public static AppState AppState { get; } = new AppState();

        public static IThemeService         ThemeService { get; } = new ThemeService();
        public static ILocalizationService  LocalisationService { get; } = new LocalizationService();
        public static IWindowService        WindowService { get; set; } = new WindowService();
        public static IDialogService        DialogService { get; set; } = new DialogService();
        public static INavigationService    NavigationService { get; } = new NavigationService(AppState);
        public static IAuthService          AuthService { get; } = new AuthService(AppState, UserRepository);
        public static IUserLibraryService   UserLibraryService { get; set; } = new UserLibraryService(UserLibraryRepository);
        public static IReaderService        ReaderService { get; set; } = new ReaderService(BookRepository, ChapterRepository, UserRepository);
        public static IBookService          BookService { get; } = new BookService(BookRepository, ChapterRepository, TagRepository, CategoryRepository);
    }
}
