using lc.Infrastructure;
using System.Windows;

namespace lc
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DatabaseInitializer.Initialize();
            base.OnStartup(e);
        }
    }
}
