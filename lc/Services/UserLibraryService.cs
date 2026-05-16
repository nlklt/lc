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

    public UserLibraryService(
        AppState appState,
        IUserLibraryListRepository listRepository,
        IUserLibraryListBookRepository listBookRepository)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _listRepository = listRepository ?? throw new ArgumentNullException(nameof(listRepository));
        _listBookRepository = listBookRepository ?? throw new ArgumentNullException(nameof(listBookRepository));
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

    public async Task EnsureDefaultListsAsync()
    {
        await _listRepository.EnsureDefaultListsAsync(CurrentUserId);
    }

    private int CurrentUserId =>
        _appState.CurrentUser?.UserId
        ?? throw new InvalidOperationException("Действие невозможно: пользователь не авторизован.");
}