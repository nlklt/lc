using lc.ViewModels.Base;

namespace lc.Services.Interfaces;

public interface INavigationService
{
    void NavigateTo<TViewModel>(params object[] args)
        where TViewModel : ViewModels.Base.ViewModelBase;

    void Navigate(ViewModels.Base.ViewModelBase viewModel);
    void NavigateBack();
}