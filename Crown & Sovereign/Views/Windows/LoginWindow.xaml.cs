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
        if(string.IsNullOrWhiteSpace(UserBox.Text)||string.IsNullOrEmpty(PasswordInput.Password)){ErrorText.Text="Escribe tu usuario y tu contraseña.";return;}
        var usuario=_autenticacion.Autenticar(UserBox.Text,PasswordInput.Password);
        if(usuario is null){ErrorText.Text="Usuario o contraseña incorrectos. Revisa tus datos e inténtalo de nuevo.";return;}
        SesionActual.Iniciar(usuario);var main=new MainWindow();Application.Current.MainWindow=main;main.Show();Close();
    }
}
