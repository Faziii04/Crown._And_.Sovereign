using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Operaciones;

public partial class ConfirmarUtilizacionDialog : Window
{
    public ConfirmarUtilizacionDialog(ActiveGuestChoice guest, ServicioModel service)
    {
        InitializeComponent(); GuestText.Text = $"{guest.Name} · Hab. {guest.Room} · {guest.Package}"; ServiceText.Text = service.Nombre;
        var stay = SampleData.Estancias.FirstOrDefault(item => item.HuespedId == guest.Id && item.Estado == "Activa");
        if (stay is not null)
        {
            var coverage = new ServicioBeneficioService().Evaluar(stay.Id, service.Id, huespedId: guest.Id);
            CoverageText.Text = $"Cobertura: {coverage.Label} · {coverage.Detail}";
            ChargeText.Text = coverage.EstimatedCharge > 0 ? $"Cargo al folio: Bs {coverage.EstimatedCharge:N2}" : "Sin cargo adicional: cobertura o compra previa aplicable";
        }
        else { CoverageText.Text = "No se encontró una estancia activa para validar cobertura."; ChargeText.Text = "Revisa la estancia antes de continuar."; }
    }
    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
