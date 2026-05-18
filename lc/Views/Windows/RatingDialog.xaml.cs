using lc.ViewModels;
using System.Windows;

namespace lc.Views.Windows;

public partial class RatingDialog : Window
{
    public RatingDialog(RatingViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}