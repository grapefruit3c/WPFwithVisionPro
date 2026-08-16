using System.Windows;
using VisionFramework.UI.Views;

namespace VisionFramework.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var login = new LoginWindow();
            if (login.ShowDialog() == true && login.LoginSuccess)
            {
                var main = new MainWindow();
                main.Show();
            }
            else
            {
                Shutdown();
            }
        }
    }
}
