IF DB_ID(N'ElectronicLibrary') IS NULL
BEGIN
    CREATE DATABASE ElectronicLibrary;
END
GO

USE ElectronicLibrary;
GO

IF OBJECT_ID(N'dbo.Books', N'U') IS NOT NULL
    DROP TABLE dbo.Books;
GO

CREATE TABLE dbo.Books
(
    BookId        INT ChapterIdENTITY(1,1) PRIMARY KEY,
    Title         NVARCHAR(200) NOT NULL,
    AuthorName    NVARCHAR(100) NOT NULL,
    Description   NVARCHAR(MAX) NULL,
    Genre         NVARCHAR(100) NULL,
    CreatedAt   DATE NULL,
    CoverImagePath NVARCHAR(260) NULL,

    Status        INT NOT NULL DEFAULT 0,
    Language      INT NOT NULL DEFAULT 0,
    AgeRating     INT NOT NULL DEFAULT 0,

    SymbolsCount  INT NOT NULL DEFAULT 0,
    Rating        DECIMAL(3,2) NOT NULL DEFAULT 0,
    Views         BIGINT NOT NULL DEFAULT 0,

    CreatedAt     DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt     DATETIME2 NULL,

    CONSTRAINT CK_Books_Rating CHECK (Rating >= 0 AND Rating <= 5),
    CONSTRAINT CK_Books_Views CHECK (Views >= 0),
    CONSTRAINT CK_Books_SymbolsCount CHECK (SymbolsCount >= 0),
    CONSTRAINT CK_Books_AgeRating CHECK (AgeRating >= 0)
);
GO

CREATE INDEX IX_Books_Title ON dbo.Books(Title);
CREATE INDEX IX_Books_AuthorName ON dbo.Books(AuthorName);
CREATE INDEX IX_Books_Genre ON dbo.Books(Genre);
CREATE INDEX IX_Books_CreatedAt ON dbo.Books(CreatedAt);
GO

CREATE OR ALTER TRIGGER dbo.trg_Books_UpdateTimestamp
ON dbo.Books
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE b
    SET UpdatedAt = SYSDATETIME()
    FROM dbo.Books b
    INNER JOIN inserted i ON b.BookId = i.BookId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Book_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        BookId,
        Title,
        AuthorName,
        Description,
        Genre,
        CreatedAt,
        CoverImagePath,
        Status,
        Language,
        AgeRating,
        SymbolsCount,
        Rating,
        Views,
        CreatedAt,
        UpdatedAt
    FROM dbo.Books
    ORDER BY Title;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Book_GetById
    @BookId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        BookId,
        Title,
        AuthorName,
        Description,
        Genre,
        CreatedAt,
        CoverImagePath,
        Status,
        Language,
        AgeRating,
        SymbolsCount,
        Rating,
        Views,
        CreatedAt,
        UpdatedAt
    FROM dbo.Books
    WHERE BookId = @BookId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Book_Search
    @SearchText NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        BookId,
        Title,
        AuthorName,
        Description,
        Genre,
        CreatedAt,
        CoverImagePath,
        Status,
        Language,
        AgeRating,
        SymbolsCount,
        Rating,
        Views,
        CreatedAt,
        UpdatedAt
    FROM dbo.Books
    WHERE
        @SearchText IS NULL
        OR LTRIM(RTRIM(@SearchText)) = N''
        OR Title LIKE N'%' + @SearchText + N'%'
        OR AuthorName LIKE N'%' + @SearchText + N'%'
        OR Genre LIKE N'%' + @SearchText + N'%'
        OR Description LIKE N'%' + @SearchText + N'%'
    ORDER BY Title;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Book_Add
    @Title NVARCHAR(200),
    @AuthorName NVARCHAR(100),
    @Description NVARCHAR(MAX) = NULL,
    @Genre NVARCHAR(100) = NULL,
    @CreatedAt DATE = NULL,
    @CoverImagePath NVARCHAR(260) = NULL,
    @Status INT = 0,
    @Language INT = 0,
    @AgeRating INT = 0,
    @SymbolsCount INT = 0,
    @Rating DECIMAL(3,2) = 0,
    @Views BIGINT = 0,
    @NewBookId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Books
    (
        Title, AuthorName, Description, Genre, CreatedAt,
        CoverImagePath, Status, Language, AgeRating,
        SymbolsCount, Rating, Views
    )
    VALUES
    (
        @Title, @AuthorName, @Description, @Genre, @CreatedAt,
        @CoverImagePath, @Status, @Language, @AgeRating,
        @SymbolsCount, @Rating, @Views
    );

    SET @NewBookId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Book_Update
    @BookId INT,
    @Title NVARCHAR(200),
    @AuthorName NVARCHAR(100),
    @Description NVARCHAR(MAX) = NULL,
    @Genre NVARCHAR(100) = NULL,
    @CreatedAt DATE = NULL,
    @CoverImagePath NVARCHAR(260) = NULL,
    @Status INT = 0,
    @Language INT = 0,
    @AgeRating INT = 0,
    @SymbolsCount INT = 0,
    @Rating DECIMAL(3,2) = 0,
    @Views BIGINT = 0
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Books
    SET
        Title = @Title,
        AuthorName = @AuthorName,
        Description = @Description,
        Genre = @Genre,
        CreatedAt = @CreatedAt,
        CoverImagePath = @CoverImagePath,
        Status = @Status,
        Language = @Language,
        AgeRating = @AgeRating,
        SymbolsCount = @SymbolsCount,
        Rating = @Rating,
        Views = @Views
    WHERE BookId = @BookId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Book_Delete
    @BookId INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Books
    WHERE BookId = @BookId;
END
GO