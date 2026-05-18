using System.Windows;
using System.Windows.Controls;
using lc.ViewModels;

namespace lc.Views.Pages;

public partial class ProfileView : UserControl
{
    public static readonly DependencyProperty AdminRequestsVMProperty =
        DependencyProperty.Register(
            nameof(AdminRequestsVM),
            typeof(AdminAuthorRequestsViewModel),
            typeof(ProfileView),
            new PropertyMetadata(null));

    public AdminAuthorRequestsViewModel? AdminRequestsVM
    {
        get => (AdminAuthorRequestsViewModel?)GetValue(AdminRequestsVMProperty);
        set => SetValue(AdminRequestsVMProperty, value);
    }

    public ProfileView()
    {
        InitializeComponent();
    }

    public ProfileView(ProfileViewModel profileVm, AdminAuthorRequestsViewModel adminRequestsVm) : this()
    {
        DataContext = profileVm;
        AdminRequestsVM = adminRequestsVm;
    }
}