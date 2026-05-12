USE eLibDb;

SELECT UserId, Role
FROM Users
WHERE Role <> 0

SELECT *
FROM Books

UPDATE Books
SET CoverImagePath = 'U:\CourseProject\lc\Data\Images\Covers\booc_cover' + 
    CAST(ABS(CHECKSUM(NEWID())) % 33 + 10 AS VARCHAR) + 
    '.jpg';
