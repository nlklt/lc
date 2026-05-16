using lc.Commands;
using lc.ViewModels.Base;
using System;
using System.Windows.Input;

namespace lc.ViewModels;

public class InputViewModel : ViewModelBase
{
    private string _inputText = string.Empty;
    private string _title = string.Empty;
    private string _message = string.Empty;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    public Action<bool>? RequestClose { get; set; }

    public ICommand AcceptCommand { get; }
    public ICommand CancelCommand { get; }

    public InputViewModel()
    {
        AcceptCommand = new RelayCommand(_ =>
        {
            RequestClose?.Invoke(true);
        });

        CancelCommand = new RelayCommand(_ =>
        {
            RequestClose?.Invoke(false);
        });
    }
}