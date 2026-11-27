using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Comercial.Convenios;

public partial class ServiciosCostosControl : UserControl
{
    private readonly ConvenioService _service = new();
    private IReadOnlyList<ServicioModel> _catalog = [];
    public event EventHandler<int>? EditAgreementRequested;
    public ServiciosCostosControl() { InitializeComponent(); DateFilter.SelectedDate = DateTime.Today; CargarCatalogo(); }
    private void CargarCatalogo()
    {
        var search = SearchBox.Text.Trim();
        var selectedId = (ServicesList.SelectedItem as ServicioModel)?.Id;
        _catalog = SampleData.Servicios.Where(item => item.Activo && item.Reservable && !SampleData.Servicios.Any(child => child.PadreId == item.Id))
            .Where(item => string.IsNullOrWhiteSpace(search) || $"{item.Nombre} {item.Categoria}".Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Categoria).ThenBy(item => item.Nombre).ToList();
        ServicesList.ItemsSource = _catalog;
        if (selectedId is int id) ServicesList.SelectedItem = _catalog.FirstOrDefault(item => item.Id == id);
        if (ServicesList.SelectedItem is null && _catalog.Count > 0) ServicesList.SelectedIndex = 0;
        CargarComparacion();
    }
    private void CargarComparacion()
    {
        if (ServicesList.SelectedItem is not ServicioModel service)
        {
            SelectedServiceTitle.Text = "Selecciona un servicio"; SelectedServiceDescription.Text = "Elige un servicio para comparar sus proveedores asociados.";
            ProvidersGrid.ItemsSource = null; ProvidersSummary.Text = ""; NoProviders.Visibility = Visibility.Visible; return;
        }
        SelectedServiceTitle.Text = service.Nombre;
        SelectedServiceDescription.Text = $"{service.Categoria} · Precio oficial al huésped: Bs {service.Precio:N2}.";
        var providers = _service.CompararProveedores(service.Id, DateFilter.SelectedDate);
        ProvidersGrid.ItemsSource = providers;
        ProvidersSummary.Text = providers.Count == 0 ? "Sin proveedores elegibles" : $"{providers.Count} alternativa(s) elegible(s) · no se selecciona una automáticamente";
        NoProviders.Visibility = providers.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (IsInitialized) CargarCatalogo(); }
    private void Service_Changed(object sender, SelectionChangedEventArgs e) { if (IsInitialized) CargarComparacion(); }
    private void Date_Changed(object sender, SelectionChangedEventArgs e) { if (IsInitialized) CargarComparacion(); }
    private void EditAgreement_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is int id) EditAgreementRequested?.Invoke(this, id); }
}
