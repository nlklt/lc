USE eLibDb;

-- 1. Заполнение Users (10 пользователей)
INSERT INTO Users (UserName, PasswordHash, Role, PreferredLanguage, PreferredTheme)
VALUES 
('Admin',			'hash_admin_123', 1, 1, 'Dark'),
('Reader_John',		'hash_pwd_2',	  0, 1, 'Light'),
('Writer_Anna',		'hash_pwd_3',	  2, 2, 'Dark'),
('BookWorm',		'hash_pwd_4',	  0, 1, 'Light'),
('Elena_V',			'hash_pwd_5',	  0, 2, 'Light'),
('Dmitry_Expert',	'hash_pwd_6',	  0, 1, 'Dark'),
('Alice_In_DB',		'hash_pwd_7',	  0, 1, 'Light'),
('Bob_The_Builder', 'hash_pwd_8',	  0, 2, 'Dark'),
('Charlie_Print',	'hash_pwd_9',	  0, 1, 'Light'),
('Denys_UA',		'hash_pwd_10',	  0, 3, 'Light');

-- 2. Заполнение Tags (10 тегов)
INSERT INTO Tags (Name) VALUES 
(N'Фэнтези'), (N'Детектив'), (N'Киберпанк'), (N'Романтика'), (N'Ужасы'), 
(N'История'), (N'Научпоп'), (N'Приключения'), (N'Драма'), (N'Комедия');

-- 3. Заполнение Categories (10 категорий)
INSERT INTO Categories (Name) VALUES 
(N'Художественная литература'), (N'Технологии'), (N'Психология'), (N'Биография'), (N'Учебники'),
(N'Классика'), (N'Комиксы'), (N'Поэзия'), (N'Триллер'), (N'Детское');

-- 4. Заполнение Books (10 книг)
INSERT INTO Books (Title, PublisherId, AuthorName, Description, BookStatus, WritingStatus, Language, AgeRating, SymbolsCount, ChaptersCount, Views, Rating)
VALUES 
(N'Путь в IT', 1, N'Дмитрий Эксперт', N'Основы баз данных для начинающих.', 1, 1, 1, 12, 50000, 5, 1200, 4.8),
(N'Тени прошлого', 2, N'Анна Райтер', N'Мрачный детектив в старом городе.', 1, 1, 1, 18, 120000, 12, 450, 4.2),
(N'Звездный скиталец', 1, N'Джек Лондон мл.', N'Научная фантастика о космосе.', 1, 0, 1, 16, 80000, 8, 3000, 4.9),
(N'Сказки на ночь', 3, N'Елена В.', N'Добрые истории для детей.', 1, 1, 1, 0, 20000, 10, 890, 4.5),
(N'Код будущего', 1, N'Кибер Пушкин', N'Мир после технологической сингулярности.', 0, 0, 2, 16, 15000, 2, 150, 3.8),
(N'Тайны океана', 4, N'Капитан Немо', N'Документальное исследование глубин.', 1, 1, 1, 6, 200000, 15, 2100, 4.7),
(N'Алгоритмы успеха', 2, N'Боб Строитель', N'Как построить карьеру.', 1, 1, 2, 12, 45000, 6, 560, 4.0),
(N'Осенний вальс', 5, N'Лириков А.', N'Сборник стихотворений.', 1, 1, 1, 12, 5000, 1, 300, 4.9),
(N'Ночной охотник', 2, N'Ван Хельсинг', N'Боевик про вампиров.', 1, 1, 1, 18, 95000, 11, 1400, 4.1),
(N'Введение в SQL', 1, N'Админ Админович', N'Справочное руководство.', 1, 1, 1, 0, 30000, 4, 5000, 5.0);

-- 5. Заполнение Chapters (по 2-3 главы для некоторых книг)
INSERT INTO Chapters (BookId, ChapterNumber, Title, Text)
VALUES 
(1, 1, N'Введение', N'Текст первой главы про таблицы...'),
(1, 2, N'Типы данных', N'Текст второй главы про INT и NVARCHAR...'),
(2, 1, N'Пролог', N'Шел сильный дождь...'),
(3, 1, N'Запуск двигателя', N'Корабль дрогнул и оторвался от земли...');

-- 6. Связи BookTags и BookCategories
INSERT INTO BookTags (BookId, TagId) VALUES (1, 7), (1, 3), (2, 2), (2, 5), (3, 1), (3, 3);
INSERT INTO BookCategories (BookId, CategoryId) VALUES (1, 2), (1, 5), (2, 9), (3, 1);

-- 7. Comments (10 комментариев)
INSERT INTO Comments (UserId, BookId, Text)
VALUES 
(2, 1, N'Очень полезно, спасибо!'),
(4, 1, N'Жду продолжения по индексам.'),
(5, 2, N'Слишком страшно на ночь.'),
(2, 3, N'Шедевр фантастики!'),
(6, 1, N'Хорошая книга для старта.'),
(7, 4, N'Детям очень понравилось.'),
(8, 7, N'Много воды, но мысли есть.'),
(9, 10, N'Лучший справочник.'),
(3, 1, N'Коллега, отличная работа!'),
(2, 5, N'Мало глав, автор пиши еще!');

-- 8. Библиотеки пользователей (UserLibraryLists)
INSERT INTO UserLibraryLists (UserId, Name)
VALUES 
(2, N'Хочу прочитать'), (2, N'Прочитано'),
(3, N'Для вдохновения'), (4, N'Техническое');

-- 9. Книги в списках (UserLibraryListBooks)
INSERT INTO UserLibraryListBooks (ListId, BookId, UserId)
VALUES 
(1, 1, 2), (1, 3, 2), (2, 4, 2), (3, 8, 3);

-- 10. Избранное (Favorites)
INSERT INTO Favorites (UserId, BookId)
VALUES (2, 1), (2, 3), (4, 1), (5, 4), (6, 10), (7, 6);

-- 11. История чтения (ReadingHistory)
INSERT INTO ReadingHistory (UserId, BookId)
VALUES (2, 1), (2, 3), (4, 1), (5, 2), (6, 10);

-- 12. Прогресс чтения (ReadingProgress)
INSERT INTO ReadingProgress (UserId, ChapterId, ProgressPercent, LastPosition)
VALUES 
(2, 1, 100, 1500),
(2, 2, 45, 500),
(4, 1, 10, 100);