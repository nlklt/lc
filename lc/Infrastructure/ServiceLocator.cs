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
        public static AppState AppState { get; } = new AppState();

        public static IChapterRepository ChapterRepository { get; } = new ChapterRepository();
        public static ICommentRepository CommentRepository { get; } = new CommentRepository();
        public static ITagRepository TagRepository { get; } = new TagRepository();
        public static ICategoryRepository CategoryRepository { get; } = new CategoryRepository();
        public static IBookRepository BookRepository { get; } = new BookRepository(ChapterRepository, CommentRepository, TagRepository, CategoryRepository);
        public static IBookService BookService { get; } = new BookService(BookRepository, ChapterRepository, TagRepository, CategoryRepository);
        public static IUserLibraryService UserLibraryService { get; set; } = null!;
        
        public static IReaderService ReaderService { get; set; } = null!;
        
        public static IDialogService DialogService { get; set; } = null!;

        public static INavigationService NavigationService { get; } =
            new NavigationService(AppState);
        public static IUserRepository UserRepository { get; } =
            new UserRepository();
        public static IThemeService ThemeService { get; } =
            new ThemeService();
        //public static ILanguageService LanguageService { get; } =
        //    new LanguageService();
        public static IAuthService AuthService { get; } =
            new AuthService(
                UserRepository,
                AppState//,
                //ThemeService,
                //LanguageService
                );
    }
}
