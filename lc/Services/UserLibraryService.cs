using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Sql;
using lc.Models;
using lc.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace lc.Services
{
    public sealed class UserLibraryService : IUserLibraryService
    {
        private readonly IUserLibraryRepository _libraryRepository;

        private readonly IUserLibraryRepository _userLibraryRepository;
        private readonly AppState _appState;

        public UserLibraryService(
            IUserLibraryRepository userLibraryRepository,
            AppState appState)
        {
            _userLibraryRepository = userLibraryRepository;
            _appState = appState;
        }


        public UserLibraryService(IUserLibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
        }
        private int CurrentUserId => ServiceLocator.AppState.CurrentUser?.UserId
            ?? throw new InvalidOperationException("Действие невозможно: пользователь не авторизован.");
        
        public Task<IReadOnlyList<UserLibraryListDto>> GetListsAsync(int userId)
        {
            return _libraryRepository.GetListsAsync(userId);
        }

        public Task<IReadOnlyList<BookListItem>> GetBooksFromListAsync(int userId, int listId)
        {
            return _libraryRepository.GetBooksFromListAsync(userId, listId);
        }

        //public async Task AddToLibraryAsync(int bookId)
        //{
        //    var lists = await _libraryRepository.GetListsAsync(CurrentUserId);

        //    var targetList = lists.FirstOrDefault()
        //        ?? throw new InvalidOperationException("У пользователя не найдено ни одного списка библиотеки.");

        //    await _libraryRepository.AddBookToListAsync(CurrentUserId, targetList.ListId, bookId);
        //}

        //public async Task RemoveFromLibraryAsync(int bookId)
        //{
        //    var lists = await _libraryRepository.GetListsAsync(CurrentUserId);
        //    foreach (var list in lists)
        //    {
        //        await _libraryRepository.RemoveBookFromListAsync(CurrentUserId, list.ListId, bookId);
        //    }
        //}

        public async Task AddToLibraryAsync(int bookId)
        {
            if (bookId <= 0)
                throw new ArgumentException("Некорректный идентификатор книги.");

            if (_appState.CurrentUser is null)
                throw new InvalidOperationException("Пользователь не авторизован.");

            var userId = _appState.CurrentUser.UserId;

            var exists = await _userLibraryRepository.IsBookInLibraryAsync(userId, bookId);

            if (exists)
                return;

            await _userLibraryRepository.AddToLibraryAsync(userId, bookId);
        }

        public async Task RemoveFromLibraryAsync(int bookId)
        {
            if (_appState.CurrentUser is null)
                throw new InvalidOperationException("Пользователь не авторизован.");

            await _userLibraryRepository.RemoveFromLibraryAsync(
                _appState.CurrentUser.UserId,
                bookId);
        }

        public async Task AddToFavoritesAsync(int bookId)
        {
            if (await IsBookFavoriteAsync(bookId)) return;

            await _libraryRepository.AddToFavoritesAsync(CurrentUserId, bookId);
        }

        public async Task RemoveFromFavoritesAsync(int bookId)
        {
            await _libraryRepository.RemoveFromFavoritesAsync(CurrentUserId, bookId);
        }


        public async Task<bool> IsBookInLibraryAsync(int bookId)
        {
            var lists = await _libraryRepository.GetListsAsync(CurrentUserId);
            foreach (var list in lists)
            {
                var books = await _libraryRepository.GetBooksFromListAsync(CurrentUserId, list.ListId);
                if (books.Any(b => b.BookId == bookId))
                    return true;
            }
            return false;
        }

        public Task<bool> IsBookFavoriteAsync(int bookId)
        {
            return _libraryRepository.IsBookFavoriteAsync(CurrentUserId, bookId);
        }

        public Task AddBookToListAsync(int userId, int listId, int bookId)
        {
            return _libraryRepository.AddBookToListAsync(userId, listId, bookId);
        }

        public Task RemoveBookFromListAsync(int userId, int listId, int bookId)
        {
            return _libraryRepository.RemoveBookFromListAsync(userId, listId, bookId);
        }
    }
}