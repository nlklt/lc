using lc.Infrastructure;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace lc.Services;

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _provider;
    private readonly AppState _appState;

    public NavigationService(IServiceProvider provider, AppState appState)
    {
        _provider = provider;
        _appState = appState;
    }

    public void NavigateTo<TViewModel>(params object[] args)
        where TViewModel : ViewModelBase
    {
        var vm = ActivatorUtilities.CreateInstance<TViewModel>(
            _provider,
            args);

        Navigate(vm);
    }

    public void Navigate(ViewModelBase viewModel)
    {
        _appState.CurrentViewModel = viewModel;
    }

    public void NavigateBack()
    {
        var prev = _appState.PrevViewModel;

        if (prev is null)
            return;

        var current = _appState.CurrentViewModel;

        _appState.CurrentViewModel = prev;

        _appState.PrevViewModel = current;
    }
}