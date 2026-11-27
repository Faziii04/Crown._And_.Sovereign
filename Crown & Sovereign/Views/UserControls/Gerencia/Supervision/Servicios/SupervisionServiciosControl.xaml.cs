using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision;

public partial class SupervisionServiciosControl : UserControl
{
    private IReadOnlyList<SupervisionServicioFila> _services = [];
    public SupervisionServiciosControl() => InitializeComponent();
    public void Cargar(SupervisionSnapshot snapshot) { _services = snapshot.Servicios; ApplyFilters(); }
    public void AbrirAlerta(SupervisionAlerta alert)
    {
        var item = _services.FirstOrDefault(row => row.Id == alert.RegistroId);
        if (item is null) return;
        ServicesGrid.SelectedItem = item; ServicesGrid.ScrollIntoView(item);
        if (item.Conflicto) ServicesTabs.SelectedIndex = 1;
        else if (item.Proveedor.Contains("proveedor", StringComparison.OrdinalIgnoreCase) || item.Proveedor.Contains("convenio", StringComparison.OrdinalIgnoreCase)) ServicesTabs.SelectedIndex = 2;
    }
    private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilters();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    private void ApplyFilters()
    {
        if (ServicesGrid is null) return;
        var query = SearchBox.Text.Trim(); var type = (TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos los tipos";
        var state = (StateBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos los estados";
        var filtered = _services.Where(item => (type == "Todos los tipos" || item.Tipo == type)
            && (state == "Todos los estados" || state == "Pendientes" && item.Estado is not ("Completada" or "Cancelada" or "COMPLETADO" or "CANCELADO") || item.Estado == state)
            && (string.IsNullOrWhiteSpace(query) || $"{item.Huesped} {item.Habitacion} {item.Servicio} {item.Proveedor} {item.Responsable} {item.Detalle}".Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();
        ServicesGrid.ItemsSource = filtered;
        ConflictGrid.ItemsSource = filtered.Where(item => item.Conflicto).ToList();
        ExternalGrid.ItemsSource = filtered.Where(item => item.Proveedor.Contains("proveedor", StringComparison.OrdinalIgnoreCase) || item.Proveedor.Contains("convenio", StringComparison.OrdinalIgnoreCase) || item.Tipo == "Traslado").ToList();
    }
}

