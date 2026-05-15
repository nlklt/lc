using lc.Data.Repositories.Interfaces;
using lc.Infrastructure;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using lc.Models.Enums;
using lc.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace lc.Services;

public sealed class CommentService : ICommentService
{
    private readonly AppState _appState;
    private readonly IBookRepository _bookRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;

    public CommentService(
        AppState appState,
        IBookRepository bookRepository,
        ICommentRepository commentRepository,
        IUserRepository userRepository)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
        _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public Task<Comment?> GetByIdAsync(int commentId)
        => _commentRepository.GetByIdAsync(commentId);

    public Task<IReadOnlyList<Comment>> GetByBookIdAsync(int bookId)
        => _commentRepository.GetByBookIdAsync(bookId);

    public async Task<int> AddAsync(int bookId, string text)
    {
        var userId = CurrentUserId;

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("Пользователь не найден.");

        if (user.BlockedComments)
            throw new InvalidOperationException("Пользователю запрещено оставлять комментарии.");

        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book is null)
            throw new InvalidOperationException($"Книга с BookId={bookId} не найдена.");

        if (book.BookStatus != BookStatus.Published)
            throw new InvalidOperationException("Комментарии можно оставлять только к опубликованной книге.");

        var comment = new Comment
        {
            BookId = bookId,
            UserId = userId,
            Text = string.IsNullOrWhiteSpace(text) ? throw new ArgumentException("Комментарий не может быть пустым.", nameof(text)) : text.Trim(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        return await _commentRepository.CreateAsync(comment);
    }

    public Task UpdateAsync(Comment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);
        return _commentRepository.UpdateAsync(comment);
    }

    public Task DeleteAsync(int commentId)
        => _commentRepository.DeleteAsync(commentId);

    private int CurrentUserId =>
        _appState.CurrentUser?.UserId
        ?? throw new InvalidOperationException("Действие невозможно: пользователь не авторизован.");
}