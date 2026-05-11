using System;
using System.Configuration;
using Microsoft.Data.SqlClient;

namespace lc.Infrastructure
{
    public static class DatabaseInitializer
    {
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["eLibDb"].ConnectionString;

        public static void Initialize()
        {
            var builder = new SqlConnectionStringBuilder(ConnectionString);
            var dbName = builder.InitialCatalog;

            if (!DatabaseExists(dbName))
            {
                CreateDatabase(dbName);
            }

            CreateTables();
        }

        private static bool DatabaseExists(string dbName)
        {
            using var connection = new SqlConnection(
                new SqlConnectionStringBuilder(ConnectionString)
                {
                    InitialCatalog = "master"
                }.ConnectionString);

            connection.Open();

            using var command = new SqlCommand(
                $"SELECT DB_ID('{dbName.Replace("'", "''")}')", connection);

            var result = command.ExecuteScalar();
            return result != null && result != DBNull.Value;
        }

        private static void CreateDatabase(string dbName)
        {
            using var connection = new SqlConnection(
                new SqlConnectionStringBuilder(ConnectionString)
                {
                    InitialCatalog = "master"
                }.ConnectionString);

            connection.Open();

            using var command = new SqlCommand(
                $"CREATE DATABASE [{dbName}]", connection);

            command.ExecuteNonQuery();

            SqlConnection.ClearAllPools();

            System.Threading.Thread.Sleep(500);
        }

        private static void CreateTables()
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
            BEGIN
                CREATE TABLE Users
                (
                    UserId INT IDENTITY(1,1) PRIMARY KEY,
                    UserName NVARCHAR(100) NOT NULL UNIQUE,
                    PasswordHash NVARCHAR(255) NOT NULL,
                    AvatarPath NVARCHAR(500) NULL,
                    BlockedComments BIT NOT NULL CONSTRAINT DF_Users_BlockedComments DEFAULT(0),
                    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT(SYSDATETIME()),
                    Role INT NOT NULL,
                    PreferredLanguage INT NOT NULL,
                    PreferredTheme NVARCHAR(50) NOT NULL CONSTRAINT DF_Users_PreferredTheme DEFAULT('Light')
                );
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Books')
            BEGIN
                CREATE TABLE Books
                (
                    BookId INT IDENTITY(1,1) PRIMARY KEY,
                    Title NVARCHAR(255) NOT NULL CONSTRAINT DF_Books_Title DEFAULT(N'Без названия'),
                    PublisherId INT NOT NULL,
                    AuthorName NVARCHAR(255),
                    Description NVARCHAR(MAX),
                    CoverImagePath NVARCHAR(500),
                    BookStatus INT NOT NULL,
                    WritingStatus INT NOT NULL,
                    Language INT,
                    AgeRating INT NOT NULL,
                    SymbolsCount BIGINT NOT NULL CONSTRAINT DF_Books_SymbolsCount DEFAULT(0),
                    ChaptersCount INT NOT NULL CONSTRAINT DF_Books_ChaptersCount DEFAULT(0),
                    Views INT NOT NULL CONSTRAINT DF_Books_Views DEFAULT(0),
                    Rating DECIMAL(4,2) NOT NULL CONSTRAINT DF_Books_Rating DEFAULT(0),
                    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Books_CreatedAt DEFAULT(SYSDATETIME()),
                    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Books_UpdatedAt DEFAULT(SYSDATETIME())
                );
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Chapters')
            BEGIN
                CREATE TABLE Chapters
                (
                    ChapterId INT IDENTITY(1,1) PRIMARY KEY,
                    BookId INT NOT NULL,
                    ChapterNumber INT NOT NULL,
                    Title NVARCHAR(255) NOT NULL,
                    Text NVARCHAR(MAX) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Chapters_CreatedAt DEFAULT(SYSDATETIME()),
                    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Chapters_UpdatedAt DEFAULT(SYSDATETIME()),
                    CONSTRAINT FK_Chapters_Books FOREIGN KEY (BookId) REFERENCES Books(BookId) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IX_Chapters_BookId_ChapterNumber
                    ON Chapters(BookId, ChapterNumber);
            END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tags')
BEGIN
    CREATE TABLE Tags
    (
        TagId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL UNIQUE
    );
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
BEGIN
    CREATE TABLE Categories
    (
        CategoryId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL UNIQUE
    );
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BookTags')
BEGIN
    CREATE TABLE BookTags
    (
        BookId INT NOT NULL,
        TagId INT NOT NULL,
        CONSTRAINT PK_BookTags PRIMARY KEY (BookId, TagId),
        CONSTRAINT FK_BookTags_Books FOREIGN KEY (BookId) REFERENCES Books(BookId) ON DELETE CASCADE,
        CONSTRAINT FK_BookTags_Tags FOREIGN KEY (TagId) REFERENCES Tags(TagId) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BookCategories')
BEGIN
    CREATE TABLE BookCategories
    (
        BookId INT NOT NULL,
        CategoryId INT NOT NULL,
        CONSTRAINT PK_BookCategories PRIMARY KEY (BookId, CategoryId),
        CONSTRAINT FK_BookCategories_Books FOREIGN KEY (BookId) REFERENCES Books(BookId) ON DELETE CASCADE,
        CONSTRAINT FK_BookCategories_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId) ON DELETE CASCADE
    );
END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Comments')
            BEGIN
                CREATE TABLE Comments
                (
                    CommentId INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT NOT NULL,
                    BookId INT NOT NULL,
                    Text NVARCHAR(MAX) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Comments_CreatedAt DEFAULT(SYSDATETIME()),
                    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Comments_UpdatedAt DEFAULT(SYSDATETIME()),
                    CONSTRAINT FK_Comments_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
                    CONSTRAINT FK_Comments_Books FOREIGN KEY (BookId) REFERENCES Books(BookId) ON DELETE CASCADE,
                );

                CREATE INDEX IX_Comments_BookId ON Comments(BookId);
                CREATE INDEX IX_Comments_UserId ON Comments(UserId);
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserLibraryLists')
            BEGIN
                CREATE TABLE UserLibraryLists (
                    ListId INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT NOT NULL,
                    Name NVARCHAR(100) NOT NULL,
                    CONSTRAINT FK_LibraryLists_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
                );
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserLibraryListBooks')
            BEGIN
                CREATE TABLE UserLibraryListBooks (
                    ListId INT NOT NULL,
                    BookId INT NOT NULL,
                    UserId INT NOT NULL,
                    AddedAt DATETIME2 DEFAULT SYSDATETIME(),
                    CONSTRAINT PK_UserLibraryListBooks PRIMARY KEY (ListId, BookId),
                    CONSTRAINT FK_ListBooks_Lists FOREIGN KEY (ListId) REFERENCES UserLibraryLists(ListId) ON DELETE CASCADE,
                    CONSTRAINT FK_ListBooks_Books FOREIGN KEY (BookId) REFERENCES Books(BookId) ON DELETE CASCADE
                );
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Favorites')
            BEGIN
                CREATE TABLE Favorites (
                    UserId INT NOT NULL,
                    BookId INT NOT NULL,
                    AddedAt DATETIME2 DEFAULT SYSDATETIME(),
                    CONSTRAINT PK_Favorites PRIMARY KEY (UserId, BookId),
                    CONSTRAINT FK_Favorites_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
                    CONSTRAINT FK_Favorites_Books FOREIGN KEY (BookId) REFERENCES Books(BookId) ON DELETE CASCADE
                );
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReadingHistory')
            BEGIN
                CREATE TABLE ReadingHistory
                (
                    HistoryId INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT NOT NULL,
                    BookId INT NOT NULL,
                    LastOpenedAt DATETIME2 NOT NULL CONSTRAINT DF_ReadingHistory_LastOpenedAt DEFAULT(SYSDATETIME()),
                    CONSTRAINT FK_ReadingHistory_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
                    CONSTRAINT FK_ReadingHistory_Books FOREIGN KEY (BookId) REFERENCES Books(BookId) ON DELETE CASCADE
                );

                CREATE INDEX IX_ReadingHistory_UserId ON ReadingHistory(UserId);
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReadingProgress')
            BEGIN
                CREATE TABLE ReadingProgress
                (
                    UserId INT NOT NULL,
                    ChapterId INT NOT NULL,
                    ProgressPercent INT NOT NULL CONSTRAINT DF_ReadingProgress_ProgressPercent DEFAULT(0),
                    LastPosition INT NOT NULL CONSTRAINT DF_ReadingProgress_LastPosition DEFAULT(0),
                    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ReadingProgress_UpdatedAt DEFAULT(SYSDATETIME()),
                    CONSTRAINT PK_ReadingProgress PRIMARY KEY (UserId, ChapterId),
                    CONSTRAINT FK_ReadingProgress_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
                    CONSTRAINT FK_ReadingProgress_Chapters FOREIGN KEY (ChapterId) REFERENCES Chapters(ChapterId) ON DELETE CASCADE
                );
            END;
";

            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}
