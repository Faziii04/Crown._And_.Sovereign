using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Operaciones.CatalogoServicios;

public partial class DisponibilidadServiciosControl : UserControl
{
    private readonly CatalogoServiciosService _service = new();
    public DisponibilidadServiciosControl() { InitializeComponent(); Loaded += (_, _) => Refrescar(); }
    public void Refrescar()
    {
        if (ReviewGrid is null) return;
        var all = _service.EstadoConfiguracion(); ReviewGrid.ItemsSource = all.Select(item => new ReadinessRow(item.Servicio, item.Categoria, item.Estado,
            item.Problemas.Count == 0 ? "Sin hallazgos de configuración detectados." : string.Join(" · ", item.Problemas))).ToList();
        SummaryText.Text = $"{all.Count(item => item.Estado.Contains("requiere revisión", StringComparison.OrdinalIgnoreCase))} servicios requieren revisión · {all.Count(item => item.Estado == "Configuración revisada")} sin hallazgos detectados.";
    }
    private void Refresh_Click(object sender, RoutedEventArgs e) => Refrescar();
    private sealed record ReadinessRow(string Servicio, string Categoria, string Estado, string ProblemText);
}
