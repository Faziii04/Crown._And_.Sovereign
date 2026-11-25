using System.Windows;
using Crown___Sovereign.Views.Windows;

namespace Crown___Sovereign;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        var login = new LoginWindow();
        MainWindow = login;
        login.Show();
    }
}
