using lc.ViewModels;
using System.Windows;

namespace lc.Views.Windows;

public partial class InputDialog : Window
{
    public InputDialog(InputViewModel vm)
    {
        InitializeComponent();

        DataContext = vm;
    }
}