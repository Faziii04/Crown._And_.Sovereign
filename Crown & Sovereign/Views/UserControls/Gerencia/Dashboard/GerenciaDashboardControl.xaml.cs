using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Dashboard;

public partial class GerenciaDashboardControl : UserControl
{
    public GerenciaDashboardControl()
    {
        InitializeComponent();
        var metrics = new DashboardService().Gerencia();
        DataContext = new
        {
            Metricas = metrics,
            Distribucion = new[] { new { Nombre = "Alojamiento", Monto = "Bs 112.400", Porcentaje = 68d }, new { Nombre = "Servicios", Monto = "Bs 31.800", Porcentaje = 42d }, new { Nombre = "Alimentos y bebidas", Monto = "Bs 24.600", Porcentaje = 34d }, new { Nombre = "Otros", Monto = "Bs 15.520", Porcentaje = 22d } },
            Autorizaciones = new[] { "2 solicitudes requieren revisión", $"{SampleData.Incidencias.Count(i => i.BloqueaHabitacion)} habitaciones bloqueadas por mantenimiento", $"{SampleData.Productos.Count(p => p.Stock <= p.StockMinimo)} productos bajo el mínimo de stock" }
        };
    }
}
