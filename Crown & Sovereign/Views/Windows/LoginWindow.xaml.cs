using System.Windows;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Windows;

public partial class LoginWindow : Window
{
    private readonly AutenticacionService _autenticacion=new();
    public LoginWindow()=>InitializeComponent();
    private void Login_Click(object sender,RoutedEventArgs e)
    {
        var usuario=_autenticacion.Autenticar(UserBox.Text,PasswordInput.Password);
        if(usuario is null){ErrorText.Text="No pudimos validar el usuario y la contraseña. Revisa los datos de demostración.";return;}
        SesionActual.Iniciar(usuario);var main=new MainWindow();Application.Current.MainWindow=main;main.Show();Close();
    }
}
