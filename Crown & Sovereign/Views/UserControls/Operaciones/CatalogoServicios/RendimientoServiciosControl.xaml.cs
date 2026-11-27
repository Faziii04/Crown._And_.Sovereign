using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Operaciones.CatalogoServicios;

public partial class RendimientoServiciosControl : UserControl
{
    public RendimientoServiciosControl() { InitializeComponent(); FromPicker.SelectedDate = DateTime.Today.AddDays(-30); ToPicker.SelectedDate = DateTime.Today; Loaded += (_, _) => Consultar(); }
    private void Refresh_Click(object sender, RoutedEventArgs e) => Consultar();
    private void Consultar()
    {
        var desde = (FromPicker.SelectedDate ?? DateTime.Today.AddDays(-30)).Date; var hasta = (ToPicker.SelectedDate ?? DateTime.Today).Date;
        if (hasta < desde) { MessageBox.Show("La fecha final no puede ser anterior a la inicial.", "Periodo inválido", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var records = SampleData.ServiciosReserva.Where(item => item.Estado == "Completado" && item.FechaUtilizacion is DateTime date && date.Date >= desde && date.Date <= hasta).ToList();
        var groups = records.GroupBy(item => item.ServicioId).Select(group =>
        {
            var revenue = group.Where(item => !item.Incluido && item.ServicioAdquiridoId is null).Sum(item => item.PrecioAplicado ?? 0m);
            var costs = group.Sum(item => item.CostoProveedorAplicado ?? 0m);
            var knownMargin = group.Where(item => !item.Incluido && item.ServicioAdquiridoId is null && item.PrecioAplicado.HasValue && item.CostoProveedorAplicado.HasValue).Sum(item => item.PrecioAplicado!.Value - item.CostoProveedorAplicado!.Value);
            return new PerformanceRow(SampleData.Servicios.FirstOrDefault(item => item.Id == group.Key)?.Nombre ?? "Servicio histórico", group.Count(), revenue,
                group.Count(item => item.Incluido || item.ServicioAdquiridoId is not null), costs, knownMargin);
        }).OrderByDescending(item => item.Usos).ThenBy(item => item.Servicio).ToList();
        PerformanceGrid.ItemsSource = groups;
        UsesText.Text = records.Count.ToString(); RevenueText.Text = $"Bs {records.Where(item => !item.Incluido && item.ServicioAdquiridoId is null).Sum(item => item.PrecioAplicado ?? 0m):N2}";
        CostText.Text = records.Where(item => item.CostoProveedorAplicado.HasValue).Sum(item => item.CostoProveedorAplicado!.Value) is var cost && records.Any(item => item.CostoProveedorAplicado.HasValue) ? $"Bs {cost:N2}" : "No registrado";
        var margins = records.Where(item => !item.Incluido && item.ServicioAdquiridoId is null && item.PrecioAplicado.HasValue && item.CostoProveedorAplicado.HasValue).ToList();
        MarginText.Text = margins.Count == 0 ? "No identificable" : $"Bs {margins.Sum(item => item.PrecioAplicado!.Value - item.CostoProveedorAplicado!.Value):N2}";
    }
    private sealed record PerformanceRow(string Servicio, int Usos, decimal Ingreso, int UsosPaquete, decimal Costo, decimal Contribucion);
}
