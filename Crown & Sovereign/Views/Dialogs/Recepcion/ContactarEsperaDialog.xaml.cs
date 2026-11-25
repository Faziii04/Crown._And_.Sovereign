using System.Windows;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class ContactarEsperaDialog : Window
{
    public string Medio => MediumBox.SelectedItem?.ToString() ?? "Teléfono";
    public string Resultado => ResultBox.SelectedItem?.ToString() ?? "Interesado";
    public string Observaciones => NotesBox.Text.Trim();
    public ContactarEsperaDialog(CoincidenciaEsperaModel match)
    {
        InitializeComponent(); GuestText.Text = $"{match.Huesped} · {match.Telefono} · {match.Correo}";
        MediumBox.ItemsSource = new[] { "Teléfono", "Correo", "Presencial" }; MediumBox.SelectedIndex = 0;
        ResultBox.ItemsSource = new[] { "Interesado", "Sin respuesta", "No interesado" }; ResultBox.SelectedIndex = 0;
    }
    private void Save_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
