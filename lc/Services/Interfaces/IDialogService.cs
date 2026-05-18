namespace lc.Services.Interfaces
{
    public interface IDialogService
    {
        Task ShowMessageAsync(string title, string message);
        Task<bool> ShowConfirmAsync(string title, string message);
        Task<string> ShowInputAsync(string title, string message, string placeholder = "");
        Task<byte?> ShowRatingAsync(string title, string message, byte initialRating = 0);

        string? OpenFile(string title, string filter);

        bool? ShowLoginDialog();
        bool? ShowRegisterDialog();
    }
}