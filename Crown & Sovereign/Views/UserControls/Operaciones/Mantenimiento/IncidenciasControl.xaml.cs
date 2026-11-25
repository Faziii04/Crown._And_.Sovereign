using Crown___Sovereign.Data;
using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;

namespace Crown___Sovereign.Views.UserControls.Operaciones.Mantenimiento;

public partial class IncidenciasControl : UserControl
{
    private readonly MantenimientoService _service = new();
    public IncidenciasControl()
    {
        InitializeComponent(); StateBox.ItemsSource = new[] { "Todos los estados", "Abierta", "En reparación", "Resuelta" }; StateBox.SelectedIndex = 0; Refresh();
    }
    private void Refresh() => IncidentGrid.ItemsSource = _service.Listar(StateBox.SelectedItem as string).Select(i => new { Incidencia = i, Habitacion = SampleData.Habitaciones.First(r => r.Id == i.Habitacion).Numero, i.Descripcion, i.Prioridad, i.Estado, Bloqueo = i.BloqueaHabitacion ? "Habitación bloqueada" : "Operativa" }).ToList();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IncidentGrid is not null) Refresh(); }
    private void Create_Click(object sender, RoutedEventArgs e) { var d = new IncidenciaDialog { Owner = Window.GetWindow(this) }; if (d.ShowDialog() == true) Refresh(); }
    private IncidenciaModel? Selected(object sender) => (sender as FrameworkElement)?.DataContext is { } row ? row.GetType().GetProperty("Incidencia")?.GetValue(row) as IncidenciaModel : null;
    private void Order_Click(object sender, RoutedEventArgs e) { if (Selected(sender) is not { } incident) return; var d = new OrdenMantenimientoDialog(incident) { Owner = Window.GetWindow(this) }; if (d.ShowDialog() == true) Refresh(); }
    private void Resolve_Click(object sender, RoutedEventArgs e) { if (Selected(sender) is not { } incident) return; _service.Resolver(incident); Refresh(); }
}
