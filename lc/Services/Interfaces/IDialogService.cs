namespace lc.Services.Interfaces
{
    public interface IDialogService
    {
        Task ShowMessageAsync(string title, string message);        // Постое соо
        Task<bool> ShowConfirmAsync(string title, string message);  // Соо об подтверждении

        Task<string?> ChooseListAsync(string title, string message, IReadOnlyList<string> options);

        bool? ShowLoginDialog();    // Окон входа
        bool? ShowRegisterDialog(); // Окно регистрации
    }
}