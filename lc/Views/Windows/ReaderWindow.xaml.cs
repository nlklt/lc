using System.Windows;
using lc.ViewModels;

namespace lc.Views.Windows;

public partial class ReaderWindow : Window
{
    public ReaderWindow(ReaderViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}