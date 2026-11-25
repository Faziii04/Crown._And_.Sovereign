using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Operaciones.Mantenimiento;

public partial class HabitacionesBloqueadasControl : UserControl
{
    public HabitacionesBloqueadasControl() { InitializeComponent(); Refresh(); }
    private void Refresh() => RoomsGrid.ItemsSource = SampleData.Habitaciones.Where(r => r.Bloqueada).ToList();
    private void Enable_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not HabitacionModel room) return;
        var incident = SampleData.Incidencias.FirstOrDefault(i => i.Habitacion == room.Id && i.BloqueaHabitacion && i.Estado != "Resuelta");
        if (incident is null) return;
        new MantenimientoService().ActualizarBloqueo(incident, false);
        Refresh();
    }
}
