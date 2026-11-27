using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision;

public partial class SupervisionMantenimientoControl : UserControl
{
    private IReadOnlyList<SupervisionIncidenciaFila> _incidents = [];
    private IReadOnlyList<SupervisionOrdenFila> _orders = [];
    private IReadOnlyList<SupervisionHabitacionFila> _rooms = [];
    public SupervisionMantenimientoControl() => InitializeComponent();
    public void Cargar(SupervisionSnapshot snapshot)
    {
        _incidents = snapshot.Incidencias; _orders = snapshot.Ordenes; _rooms = snapshot.Habitaciones;
        OrderGrid.ItemsSource = _orders; BlockedGrid.ItemsSource = _rooms.Where(item => item.BloqueoActivo).ToList();
        var closed = _incidents.Count(item => item.Estado == "Resuelta"); var open = _incidents.Count - closed;
        var urgent = _incidents.Count(item => item.Estado != "Resuelta" && item.Prioridad is "Urgente" or "Alta");
        PerformanceText.Text = $"Incidencias registradas en el periodo: {_incidents.Count}. Abiertas: {open}. Resueltas: {closed}. Prioridad alta o urgente: {urgent}. Órdenes visibles: {_orders.Count}; estimación vencida: {_orders.Count(item => item.Vencida)}.\n\nEl tiempo de resolución y el tiempo efectivo de reparación no se calculan sin fechas reales de resolución/inicio/fin. La habitación bloqueada se cuenta por el estado actual de la habitación.";
        ApplyFilters();
    }
    public void AbrirAlerta(SupervisionAlerta alert)
    {
        var incident = _incidents.FirstOrDefault(item => item.Id == alert.RegistroId);
        if (incident is not null) { IncidentGrid.SelectedItem = incident; IncidentGrid.ScrollIntoView(incident); return; }
        var order = _orders.FirstOrDefault(item => item.Id == alert.RegistroId);
        if (order is not null) { MaintenanceTabs.SelectedIndex = 1; OrderGrid.SelectedItem = order; OrderGrid.ScrollIntoView(order); }
    }
    private void Incident_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilters();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    private void ApplyFilters()
    {
        if (IncidentGrid is null) return;
        var text = SearchBox.Text.Trim(); var priority = (PriorityBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todas las prioridades";
        var state = (StateBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos los estados";
        IncidentGrid.ItemsSource = _incidents.Where(item => (priority == "Todas las prioridades" || item.Prioridad == priority)
            && (state == "Todos los estados" || state == "Abiertas" && item.Estado != "Resuelta" || state == "Resueltas" && item.Estado == "Resuelta")
            && (string.IsNullOrWhiteSpace(text) || $"{item.Habitacion} {item.Descripcion} {item.Categoria}".Contains(text, StringComparison.OrdinalIgnoreCase))).ToList();
    }
}

