using lc.Infrastructure;
using System.Windows;

namespace lc
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DatabaseInitializer.Initialize();
        }
    }
}
