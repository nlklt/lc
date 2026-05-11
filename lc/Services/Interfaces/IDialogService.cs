namespace lc.Services.Interfaces
{
    public interface IDialogService
    {
        Task ShowMessageAsync(string title, string message);
        Task<bool> ShowConfirmAsync(string title, string message);
        Task<string?> ChooseListAsync(string title, string message, IReadOnlyList<string> options);
    }
}