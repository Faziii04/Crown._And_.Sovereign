using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Dashboard;

public partial class DashboardOperacionControl : UserControl
{
    public event EventHandler? ModuleRequested;
    public event EventHandler<string>? SectionRequested;
    public DashboardOperacionControl() => InitializeComponent();
    public void Cargar(IReadOnlyList<DashboardMetric> metrics, IReadOnlyList<DashboardAlert> alerts) { MetricList.ItemsSource = metrics; AlertList.ItemsSource = alerts; }
    private void Open_Click(object sender, RoutedEventArgs e) => ModuleRequested?.Invoke(this, EventArgs.Empty);
    private void Metric_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag?.ToString() is not string title) return;
        var section = title switch
        {
            "Habitaciones listas" or "Ingresos en riesgo" or "Habitaciones bloqueadas" => "Habitaciones",
            "Incidencias prioritarias" => "Mantenimiento", "Servicios por coordinar" => "Servicios",
            "Artículos bajo mínimo" => "Inventario y recursos", _ => "Limpieza"
        };
        SectionRequested?.Invoke(this, section);
    }
}
