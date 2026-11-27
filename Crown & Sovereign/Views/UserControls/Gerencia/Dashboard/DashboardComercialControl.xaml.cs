using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Dashboard;

public partial class DashboardComercialControl : UserControl
{
    public event EventHandler<string>? NavigationRequested;
    public DashboardComercialControl() => InitializeComponent();
    public void Cargar(IReadOnlyList<DashboardMetric> metrics, IReadOnlyList<DashboardBar> channels)
    {
        MetricList.ItemsSource = metrics;
        var total = Math.Max(1, channels.Sum(item => item.Valor));
        ChannelList.ItemsSource = channels.Select(item => item with { Detalle = $"{item.Valor} reservas · {(double)(item.Valor * 100m / total):N0}%" }).ToList();
    }
    private void Metric_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if ((sender as System.Windows.Controls.Button)?.Tag?.ToString() == "Convenios por vencer") NavigationRequested?.Invoke(this, "Convenios");
    }
}
