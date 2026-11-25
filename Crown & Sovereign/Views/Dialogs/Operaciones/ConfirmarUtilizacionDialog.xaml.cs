using System.Windows;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.Dialogs.Operaciones;

public partial class ConfirmarUtilizacionDialog : Window
{
    public ConfirmarUtilizacionDialog(ActiveGuestChoice guest, ServicioModel service)
    {
        InitializeComponent(); GuestText.Text = $"{guest.Name} · Hab. {guest.Room} · {guest.Package}"; ServiceText.Text = service.Nombre; CoverageText.Text = $"Cobertura actual: {service.Cobertura}"; ChargeText.Text = service.Cobertura == "No incluido" ? $"Cargo al folio: Bs {service.Precio:N2}" : "Uso incluido en el paquete";
    }
    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
