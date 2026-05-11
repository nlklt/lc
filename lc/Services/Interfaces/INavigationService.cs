using lc.ViewModels.Base;

namespace lc.Services.Interfaces
{
    public interface INavigationService
    {
        void Navigate(ViewModelBase viewModel);
    }
}
