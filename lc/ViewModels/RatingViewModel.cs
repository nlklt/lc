using lc.Commands;
using lc.ViewModels.Base;
using System;
using System.Windows.Input;

namespace lc.ViewModels;

public sealed class RatingViewModel : ViewModelBase
{
    private byte _selectedRating;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public Action<bool?>? RequestClose { get; set; }

    public byte SelectedRating
    {
        get => _selectedRating;
        set
        {
            var normalized = value is >= 1 and <= 5 ? value : (byte)0;

            if (SetProperty(ref _selectedRating, normalized))
            {
                OnPropertyChanged(nameof(SelectedRatingText));

                if (AcceptCommand is RelayCommand accept)
                    accept.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedRatingText =>
        SelectedRating == 0 ? "Оценка не выбрана" : $"Выбрано: {SelectedRating} / 5";

    public ICommand SetRatingCommand { get; }
    public ICommand AcceptCommand { get; }
    public ICommand CancelCommand { get; }

    public RatingViewModel()
    {
        SetRatingCommand = new RelayCommand(param =>
        {
            if (TryParseRating(param, out var rating))
                SelectedRating = rating;
        });

        AcceptCommand = new RelayCommand(
            _ => RequestClose?.Invoke(true),
            _ => SelectedRating is >= 1 and <= 5);

        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    private static bool TryParseRating(object? parameter, out byte rating)
    {
        rating = 0;

        if (parameter is byte b && b is >= 1 and <= 5)
        {
            rating = b;
            return true;
        }

        if (parameter is int i && i is >= 1 and <= 5)
        {
            rating = (byte)i;
            return true;
        }

        if (parameter is string s && byte.TryParse(s, out var parsed) && parsed is >= 1 and <= 5)
        {
            rating = parsed;
            return true;
        }

        return false;
    }
}