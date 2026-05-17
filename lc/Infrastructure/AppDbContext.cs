using lc.Models;
using lc.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace lc.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<BookTag> BookTags => Set<BookTag>();
    public DbSet<BookCategory> BookCategories => Set<BookCategory>();

    public DbSet<BookView> BookViews => Set<BookView>();
    public DbSet<BookRating> BookRatings => Set<BookRating>();

    public DbSet<UserLibraryList> UserLibraryLists => Set<UserLibraryList>();
    public DbSet<UserLibraryListBook> UserLibraryListBooks => Set<UserLibraryListBook>();

    public DbSet<ReadingHistory> ReadingHistories => Set<ReadingHistory>();
    public DbSet<ReadingProgress> ReadingProgresses => Set<ReadingProgress>();

    public DbSet<AuthorRequest> AuthorRequests => Set<AuthorRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.UserId);

            entity.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.UserName).IsUnique();

            entity.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.AvatarPath).HasMaxLength(500);

            entity.Property(x => x.BlockedComments).HasDefaultValue(false);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(x => x.PreferredTheme).HasMaxLength(50).HasDefaultValue("Dark");

            entity.Property(x => x.Role).HasConversion<int>().IsRequired();
            entity.Property(x => x.PreferredLanguage).HasConversion<int>().IsRequired();

            entity.HasMany(x => x.PublishedBooks)
                .WithOne(x => x.Publisher)
                .HasForeignKey(x => x.PublisherId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("Books");
            entity.HasKey(x => x.BookId);

            entity.Property(x => x.Title)
                .HasMaxLength(255)
                .IsRequired()
                .HasDefaultValue("Без названия");

            entity.Property(x => x.AuthorName).HasMaxLength(255);
            entity.Property(x => x.Description);
            entity.Property(x => x.CoverImagePath).HasMaxLength(500);

            entity.Property(x => x.BookStatus).HasConversion<int>().IsRequired();
            entity.Property(x => x.WritingStatus).HasConversion<int>().IsRequired();
            entity.Property(x => x.Language).HasConversion<int>().IsRequired();
            entity.Property(x => x.AgeRating).IsRequired();

            entity.Property(x => x.SymbolsCount).HasDefaultValue(0L);
            entity.Property(x => x.ChaptersCount).HasDefaultValue(0);
            entity.Property(x => x.Views).HasDefaultValue(0L);
            entity.Property(x => x.Rating).HasPrecision(4, 2).HasDefaultValue(0m);

            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSDATETIME()");

            entity.HasIndex(x => x.PublisherId);

            entity.HasMany(x => x.Chapters)
                .WithOne(x => x.Book)
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Comments)
                .WithOne(x => x.Book)
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.BookViews)
                .WithOne(x => x.Book)
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.BookRatings)
                .WithOne(x => x.Book)
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Tags)
                .WithMany(x => x.Books)
                .UsingEntity<BookTag>(
                    j => j.HasOne(x => x.Tag)
                          .WithMany()
                          .HasForeignKey(x => x.TagId)
                          .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne(x => x.Book)
                          .WithMany()
                          .HasForeignKey(x => x.BookId)
                          .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.ToTable("BookTags");
                        j.HasKey(x => new { x.BookId, x.TagId });
                    });

            entity.HasMany(x => x.Categories)
                .WithMany(x => x.Books)
                .UsingEntity<BookCategory>(
                    j => j.HasOne(x => x.Category)
                          .WithMany()
                          .HasForeignKey(x => x.CategoryId)
                          .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne(x => x.Book)
                          .WithMany()
                          .HasForeignKey(x => x.BookId)
                          .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.ToTable("BookCategories");
                        j.HasKey(x => new { x.BookId, x.CategoryId });
                    });
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.ToTable("Chapters");
            entity.HasKey(x => x.ChapterId);

            entity.Property(x => x.ChapterNumber).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Text).IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired()
                .HasDefaultValue(ChapterStatus.Draft);

            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSDATETIME()");

            entity.HasIndex(x => new { x.BookId, x.ChapterNumber }).IsUnique();
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("Comments");
            entity.HasKey(x => x.CommentId);

            entity.Property(x => x.Text).IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(x => x.User)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Book)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.BookId);
            entity.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tags");
            entity.HasKey(x => x.TagId);

            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(x => x.CategoryId);

            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<BookView>(entity =>
        {
            entity.ToTable("BookViews");
            entity.HasKey(x => new { x.BookId, x.UserId });

            entity.Property(x => x.ViewedAt).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(x => x.User)
                .WithMany(x => x.BookViews)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Book)
                .WithMany(x => x.BookViews)
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookRating>(entity =>
        {
            entity.ToTable("BookRatings");
            entity.HasKey(x => new { x.BookId, x.UserId });

            entity.Property(x => x.Rating).IsRequired();
            entity.Property(x => x.RatedAt).HasDefaultValueSql("SYSDATETIME()");

            entity.HasCheckConstraint("CK_BookRatings_Rating", "[Rating] BETWEEN 1 AND 5");

            entity.HasOne(x => x.User)
                .WithMany(x => x.BookRatings)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Book)
                .WithMany(x => x.BookRatings)
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserLibraryList>(entity =>
        {
            entity.ToTable("UserLibraryLists");
            entity.HasKey(x => x.ListId);

            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.Name }).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.LibraryLists)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserLibraryListBook>(entity =>
        {
            entity.ToTable("UserLibraryListBooks");
            entity.HasKey(x => new { x.ListId, x.BookId });

            entity.Property(x => x.AddedAt).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(x => x.List)
                .WithMany(x => x.Books)
                .HasForeignKey(x => x.ListId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Book)
                .WithMany()
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReadingHistory>(entity =>
        {
            entity.ToTable("ReadingHistories");
            entity.HasKey(x => x.HistoryId);

            entity.Property(x => x.LastOpenedAt).HasDefaultValueSql("SYSDATETIME()");

            entity.HasIndex(x => new { x.UserId, x.BookId }).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.ReadingHistory)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Book)
                .WithMany()
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReadingProgress>(entity =>
        {
            entity.ToTable("ReadingProgress");
            entity.HasKey(x => new { x.UserId, x.ChapterId });

            entity.Property(x => x.ProgressPercent).HasDefaultValue(0);
            entity.Property(x => x.LastPosition).HasDefaultValue(0);
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(x => x.User)
                .WithMany(x => x.ReadingProgresses)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Book)
                .WithMany()
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Chapter)
                .WithMany()
                .HasForeignKey(x => x.ChapterId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(x => new { x.BookId, x.ChapterId });
        });

        modelBuilder.Entity<AuthorRequest>(entity =>
        {
            entity.ToTable("AuthorRequests");
            entity.HasKey(x => x.RequestId);

            entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();

            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(x => x.ReviewedAt);
            entity.Property(x => x.ReviewComment).HasMaxLength(2000);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Reviewer)
                .WithMany()
                .HasForeignKey(x => x.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.UserId, x.Status });
            entity.HasIndex(x => x.Status);
        });
    }
}