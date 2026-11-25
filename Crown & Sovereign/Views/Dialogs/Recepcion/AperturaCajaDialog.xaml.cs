using System.Globalization;
using System.Windows;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class AperturaCajaDialog : Window
{
    public AperturaCajaDialog()
    {
        InitializeComponent();
        UserText.Text = SesionActual.Usuario?.Nombre ?? "—";
        DateText.Text = DateTime.Now.ToString("dd MMM yyyy · HH:mm", CultureInfo.GetCultureInfo("es-BO"));
    }
    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(OpeningBox.Text, NumberStyles.Number, new CultureInfo("es-BO"), out var amount)) { ValidationText.Text = "Ingresa un importe válido."; return; }
        try { new CajaService().Abrir(amount, NotesBox.Text.Trim()); DialogResult = true; }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
