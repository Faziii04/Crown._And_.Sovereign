using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Dashboard;

public partial class DashboardOperacionControl : UserControl
{
    public event EventHandler? ModuleRequested;
    public DashboardOperacionControl() => InitializeComponent();
    public void Cargar(IReadOnlyList<DashboardMetric> metrics, IReadOnlyList<DashboardAlert> alerts) { MetricList.ItemsSource = metrics; AlertList.ItemsSource = alerts; }
    private void Open_Click(object sender, RoutedEventArgs e) => ModuleRequested?.Invoke(this, EventArgs.Empty);
}
