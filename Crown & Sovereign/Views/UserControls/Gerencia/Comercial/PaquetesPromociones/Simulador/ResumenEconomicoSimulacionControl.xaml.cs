using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial.PaquetesPromociones.Simulador;

public partial class ResumenEconomicoSimulacionControl : UserControl
{
    public event EventHandler? CompararSolicitado;
    public ResumenEconomicoSimulacionControl() => InitializeComponent();
    public void Mostrar(ResumenEconomicoReserva summary, string warning, IReadOnlyList<string> benefits)
    {
        TotalText.Text = $"Bs {summary.Total:N2}"; LodgingText.Text = $"Bs {summary.Alojamiento:N2}"; PackagesText.Text = $"Bs {summary.Paquetes:N2}";
        ResourcesText.Text = $"Bs {summary.Recursos:N2}"; ServicesText.Text = $"Bs {summary.Servicios:N2}"; DiscountText.Text = $"− Bs {summary.Descuentos:N2}"; TaxText.Text = $"Bs {summary.Impuestos:N2}";
        WarningText.Text = warning; IncludedBenefits.ItemsSource = benefits; ComparisonText.Text = "";
    }
    public void MostrarError(string message) { TotalText.Text = "—"; WarningText.Text = message; }
    public void MostrarComparacion(string message) => ComparisonText.Text = message;
    private void Compare_Click(object sender, RoutedEventArgs e) => CompararSolicitado?.Invoke(this, EventArgs.Empty);
}
