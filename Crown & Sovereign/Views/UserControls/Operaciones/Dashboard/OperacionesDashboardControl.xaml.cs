using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Operaciones.Dashboard;

public partial class OperacionesDashboardControl : UserControl
{
    public event EventHandler<string>? NavigationRequested;
    public OperacionesDashboardControl()
    {
        InitializeComponent();
        var dashboard = new DashboardService();
        DataContext = new { Metricas = dashboard.Operaciones(), Alertas = dashboard.AlertasOperacion(), Citas = SampleData.Citas.OrderBy(c => c.Inicio).Take(10).Select(c => { var guest = SampleData.Huespedes.First(g => g.Id == c.HuespedId); return new { c.Inicio, Servicio = SampleData.Servicios.First(s => s.Id == c.ServicioId).Nombre, Huesped = guest.Nombre, Habitacion = SampleData.Habitaciones.First(r => r.Id == guest.Habitacion).Numero, c.Estado }; }).ToList() };
    }
    private void Navigate_Click(object sender, RoutedEventArgs e) { if (sender is Button { Tag: string module }) NavigationRequested?.Invoke(this, module); }
}
