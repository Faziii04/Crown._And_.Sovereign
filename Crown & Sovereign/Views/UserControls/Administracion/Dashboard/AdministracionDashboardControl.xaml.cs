using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Administracion.Dashboard;

public partial class AdministracionDashboardControl : UserControl
{
    public AdministracionDashboardControl()
    {
        InitializeComponent();
        DataContext = new { Metricas = new DashboardService().Administracion(), Roles = SampleData.Usuarios.GroupBy(u => u.Rol).Select(g => new { Rol = g.Key, Cantidad = g.Count() }), Auditoria = SampleData.Auditoria.Take(20) };
    }
}
