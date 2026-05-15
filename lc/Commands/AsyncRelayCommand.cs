using System.Diagnostics;
using System.Windows.Input;

namespace lc.Commands;

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Predicate<object?>? _canExecute;
    private readonly Action<Exception>? _onException;

    private int _isExecuting;

    public AsyncRelayCommand(
        Func<object?, Task> execute,
        Predicate<object?>? canExecute = null,
        Action<Exception>? onException = null)
    {
        ArgumentNullException.ThrowIfNull(execute);

        _execute = execute;
        _canExecute = canExecute;
        _onException = onException;
    }

    public AsyncRelayCommand(
        Func<Task> execute,
        Func<bool>? canExecute = null,
        Action<Exception>? onException = null) : this(
            _ => execute(),
            canExecute is null ? null : _ => canExecute(),
            onException)  { }

    public bool CanExecute(object? parameter)
        => Volatile.Read(ref _isExecuting) == 0 &&
           (_canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter)
    {
        if (!TryBeginExecute())
            return;

        try
        {
            await _execute(parameter);
        }
        catch (Exception ex)
        {
            _onException?.Invoke(ex);
            Debug.WriteLine(ex);
        }
        finally
        {
            EndExecute();
        }
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private bool TryBeginExecute()
    {
        if (Interlocked.CompareExchange(ref _isExecuting, 1, 0) != 0)
            return false;

        RaiseCanExecuteChanged();
        return true;
    }

    private void EndExecute()
    {
        Interlocked.Exchange(ref _isExecuting, 0);
        RaiseCanExecuteChanged();
    }
}