using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial;

public partial class PaquetesPromocionesControl : UserControl
{
    public PaquetesPromocionesControl()
    {
        InitializeComponent();
        PackagesView.CatalogoCambiado += (_, _) => ActualizarResumen();
        PromotionsView.CatalogoCambiado += (_, _) => ActualizarResumen();
        ActualizarResumen();
    }
    private void ActualizarResumen()
    {
        var today = DateTime.Today;
        var available = SampleData.Paquetes.Count(item => item.Activo);
        var activePromotions = SampleData.Promociones.Where(item => item.Activa && item.Desde.Date <= today && item.Hasta.Date >= today).ToList();
        var expiring = SampleData.Promociones.Count(item => item.Activa && item.Hasta.Date >= today && item.Hasta.Date <= today.AddDays(30));
        var packageIssues = SampleData.Paquetes.Count(package => !package.Activo || !SampleData.PaquetesServicios.Any(item => item.PaqueteId == package.Id) || SampleData.PaquetesServicios.Any(benefit => benefit.PaqueteId == package.Id && !SampleData.Servicios.Any(service => service.Id == benefit.ServicioId && service.Activo && service.Reservable)));
        var promoIssues = SampleData.Promociones.Count(item => item.Hasta.Date >= today && !item.TipoDescuento.Equals("Porcentaje", StringComparison.OrdinalIgnoreCase));
        Overview.Actualizar(available, activePromotions.Count, expiring, packageIssues + promoIssues);
    }
    private void NewPackage_Click(object sender, RoutedEventArgs e) { WorkspaceTabs.SelectedIndex = 0; PackagesView.CrearNuevo(); ActualizarResumen(); }
    private void NewPromotion_Click(object sender, RoutedEventArgs e) { WorkspaceTabs.SelectedIndex = 1; PromotionsView.NuevaPromocion(); ActualizarResumen(); }
    private void Simulate_Click(object sender, RoutedEventArgs e) { WorkspaceTabs.SelectedIndex = 2; }
}
