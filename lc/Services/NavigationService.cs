using lc.Infrastructure;
using lc.Services.Interfaces;
using lc.ViewModels.Base;

namespace lc.Services
{
    public class NavigationService : INavigationService
    {
        private readonly AppState _appState;

        public NavigationService(AppState appState)
        {
            _appState = appState;
        }

        public void Navigate(ViewModelBase viewModel)
        {
            if (!CanNavigate(viewModel))
                return;

            _appState.CurrentViewModel = viewModel;
        }

        private bool CanNavigate(ViewModelBase viewModel)
        {
            if (viewModel is null)
                return false;

            if (viewModel.GetType().Name == "LoginViewModel")
                return true;

            if (viewModel.GetType().Name == "RegisterViewModel")
                return true;

            if (viewModel.GetType().Name == "EditBookViewModel")
                return _appState.CanManageBooks;

            if (viewModel.GetType().Name == "ReaderViewModel")
                return _appState.IsAuthenticated;

            if (viewModel.GetType().Name == "ProfileViewModel")
                return _appState.IsAuthenticated;

            return true;
        }
    }
}
