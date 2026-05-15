using lc.Data.Repositories.Interfaces;
using lc.Helpers;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Services.Interfaces;

namespace lc.Services;

public sealed class UserLibraryService : IUserLibraryService
{
    private readonly AppState _appState;
    private readonly IUserLibraryListRepository _listRepository;
    private readonly IUserLibraryListBookRepository _listBookRepository;
    private readonly IFavoriteRepository _favoriteRepository;

    public UserLibraryService(
        AppState appState,
        IUserLibraryListRepository listRepository,
        IUserLibraryListBookRepository listBookRepository,
        IFavoriteRepository favoriteRepository)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _listRepository = listRepository ?? throw new ArgumentNullException(nameof(listRepository));
        _listBookRepository = listBookRepository ?? throw new ArgumentNullException(nameof(listBookRepository));
        _favoriteRepository = favoriteRepository ?? throw new ArgumentNullException(nameof(favoriteRepository));
    }

    public Task<IReadOnlyList<UserLibraryListDto>> GetListsAsync()
        => _listRepository.GetListsAsync(CurrentUserId);

    public Task<int> CreateListAsync(string name)
        => _listRepository.CreateAsync(CurrentUserId, name);

    public Task RenameListAsync(int listId, string name)
        => _listRepository.RenameAsync(CurrentUserId, listId, name);

    public Task DeleteListAsync(int listId)
        => _listRepository.DeleteAsync(CurrentUserId, listId);

    public Task<IReadOnlyList<BookListItemDto>> GetBooksFromListAsync(int listId)
        => _listBookRepository.GetBooksAsync(CurrentUserId, listId);

    public Task AddBookToListAsync(int listId, int bookId)
        => _listBookRepository.AddBookAsync(CurrentUserId, listId, bookId);

    public Task RemoveBookFromListAsync(int listId, int bookId)
        => _listBookRepository.RemoveBookAsync(CurrentUserId, listId, bookId);

    public async Task AddToLibraryAsync(int bookId)
    {
        var listId = await GetOrCreateDefaultLibraryListIdAsync();
        await _listBookRepository.AddBookAsync(CurrentUserId, listId, bookId);
    }

    public async Task RemoveFromLibraryAsync(int bookId)
    {
        var lists = await _listRepository.GetListsAsync(CurrentUserId);

        foreach (var list in lists)
        {
            if (await _listBookRepository.ExistsAsync(CurrentUserId, list.ListId, bookId))
                await _listBookRepository.RemoveBookAsync(CurrentUserId, list.ListId, bookId);
        }
    }

    public Task AddToFavoritesAsync(int bookId)
        => _favoriteRepository.AddAsync(CurrentUserId, bookId);

    public Task RemoveFromFavoritesAsync(int bookId)
        => _favoriteRepository.RemoveAsync(CurrentUserId, bookId);

    public async Task<bool> IsBookInLibraryAsync(int bookId)
    {
        var lists = await _listRepository.GetListsAsync(CurrentUserId);

        foreach (var list in lists)
        {
            if (await _listBookRepository.ExistsAsync(CurrentUserId, list.ListId, bookId))
                return true;
        }

        return false;
    }

    public Task<bool> IsBookFavoriteAsync(int bookId)
        => _favoriteRepository.ExistsAsync(CurrentUserId, bookId);

    private async Task<int> GetOrCreateDefaultLibraryListIdAsync()
    {
        var lists = await _listRepository.GetListsAsync(CurrentUserId);

        var library = lists.FirstOrDefault(x => x.Name == "Библиотека");
        if (library is not null)
            return library.ListId;

        return await _listRepository.CreateAsync(CurrentUserId, "Библиотека");
    }

    private int CurrentUserId =>
        _appState.CurrentUser?.UserId
        ?? throw new InvalidOperationException("Действие невозможно: пользователь не авторизован.");
}