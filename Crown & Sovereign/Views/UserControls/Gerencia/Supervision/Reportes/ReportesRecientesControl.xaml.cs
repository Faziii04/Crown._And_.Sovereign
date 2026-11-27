using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision.Reportes;

public sealed record ReporteRecienteFila(string Titulo, string Periodo, DateTime Generado, string Estado, ResultadoReporte Resultado, ParametrosReporte Parametros);
public partial class ReportesRecientesControl : UserControl
{
    public event Action<ResultadoReporte>? ConsultarSolicitado;
    public event Action<ResultadoReporte, ParametrosReporte>? ReutilizarSolicitado;
    public ReportesRecientesControl() { InitializeComponent(); Refrescar(); }
    public void Refrescar() => RecentGrid.ItemsSource = ReporteService.Recientes.Select(x => new ReporteRecienteFila(x.Titulo, $"{x.Desde:dd/MM} – {x.Hasta:dd/MM/yyyy}", x.Generado, "Disponible", x, new(x.Id, "Periodo personalizado", x.Desde, x.Hasta))).ToList();
    private ReporteRecienteFila? Selected => RecentGrid.SelectedItem as ReporteRecienteFila;
    private void Selection_Changed(object sender, SelectionChangedEventArgs e) { }
    private void Open_Click(object sender, RoutedEventArgs e) { if (Selected is { } row) ConsultarSolicitado?.Invoke(row.Resultado); }
    private void Reuse_Click(object sender, RoutedEventArgs e) { if (Selected is { } row) ReutilizarSolicitado?.Invoke(row.Resultado, row.Parametros); }
}
