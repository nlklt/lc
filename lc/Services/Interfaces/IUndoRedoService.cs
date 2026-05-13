using lc.Data.Repositories.Interfaces;
using lc.Models.Enums;
using lc.Services.Interfaces;
using System.Collections.Generic;

public interface IUndoRedoService
{
    bool CanUndo { get; }
    bool CanRedo { get; }

    void Push(IUndoableAction action);
    Task UndoAsync();
    Task RedoAsync();
}

public sealed class UndoRedoService : IUndoRedoService
{
    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Push(IUndoableAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear();
    }

    public async Task UndoAsync()
    {
        if (_undoStack.Count == 0)
            return;

        var action = _undoStack.Pop();
        await action.UndoAsync();
        _redoStack.Push(action);
    }

    public async Task RedoAsync()
    {
        if (_redoStack.Count == 0)
            return;

        var action = _redoStack.Pop();
        await action.RedoAsync();
        _undoStack.Push(action);
    }

    public sealed class ArchiveBookAction : IUndoableAction
    {
        private readonly IBookRepository _bookRepository;
        private readonly int _bookId;
        private readonly BookStatus _previousStatus;

        public ArchiveBookAction(IBookRepository bookRepository, int bookId, BookStatus previousStatus)
        {
            _bookRepository = bookRepository;
            _bookId = bookId;
            _previousStatus = previousStatus;
        }

        public Task UndoAsync()
        {
            return _bookRepository.UpdateStatusAsync(_bookId, _previousStatus);
        }

        public Task RedoAsync()
        {
            return _bookRepository.UpdateStatusAsync(_bookId, BookStatus.Archived);
        }
    }
}