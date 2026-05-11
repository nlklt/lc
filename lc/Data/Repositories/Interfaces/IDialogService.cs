namespace lc.Services.Interfaces
{
    public interface IDialogService
    {
        Task<bool> ConfirmAsync(string title, string message);
        Task<string?> ChooseListAsync(string title, string message, IReadOnlyList<string> options);
    }
}