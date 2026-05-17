using lc.Data.Repositories;
using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Infrastructure.Repositories.Sql;
using lc.Services;
using lc.Services.Interfaces;
using lc.ViewModels;
using lc.Views;
using lc.Views.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace lc;

public partial class App : Application
{
    public static IHost AppHost { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        AppHost = Host
            .CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .Build();

        await AppHost.StartAsync();

        using var scope = AppHost.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        var mainWindow =AppHost.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (AppHost)
        {
            await AppHost.StopAsync();
        }

        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(ConnectionStrings.ELibDb);
        });

        services.AddSingleton<AppState>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IChapterRepository, ChapterRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();

        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        services.AddScoped<IUserLibraryListRepository, UserLibraryListRepository>();
        services.AddScoped<IUserLibraryListBookRepository, UserLibraryListBookRepository>();

        services.AddScoped<IBookRatingRepository, BookRatingRepository>();
        services.AddScoped<IBookViewRepository, BookViewRepository>();

        services.AddScoped<IReadingHistoryRepository, ReadingHistoryRepository>();
        services.AddScoped<IReadingProgressRepository, ReadingProgressRepository>();
        
        services.AddScoped<IAuthorRequestRepository, AuthorRequestRepository>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IBookStatsService, BookStatsService>();

        services.AddScoped<IChapterService, ChapterService>();
        services.AddScoped<ICommentService, CommentService>();

        services.AddScoped<IReaderService, ReaderService>();
        services.AddScoped<IUserLibraryService, UserLibraryService>();
        services.AddScoped<IReadingProgressService, ReadingProgressService>();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IWindowService, WindowService>();

        services.AddScoped<IAuthorRequestService, AuthorRequestService>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<CatalogViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<BookDetailsViewModel>();
        services.AddTransient<EditBookViewModel>();
        services.AddTransient<ReaderViewModel>();
        services.AddTransient<EditChapterViewModel>();
        services.AddTransient<NavigationViewModel>();
        services.AddTransient<InputViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<RegisterWindow>();
        services.AddTransient<ReaderWindow>();

        services.AddTransient<InputDialog>();
    }
}