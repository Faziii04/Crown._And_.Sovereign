using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Dashboard;

public partial class DashboardFinanzasControl : UserControl
{
    public DashboardFinanzasControl() => InitializeComponent();
    public void Cargar(DashboardExecutiveSummary summary)
    {
        TotalText.Text = $"Total: Bs {summary.Ingresos:N2} · Pagos recibidos: Bs {summary.Pagos:N2}";
        TrendList.ItemsSource = summary.Tendencia;
        CategoryList.ItemsSource = summary.Distribucion;
        CashText.Text = $"Pagos recibidos: Bs {summary.Pagos:N2} · Saldo actual de folios: Bs {summary.SaldoActualFolios:N2}. Correcciones ejecutadas en el período: devoluciones Bs {summary.Devoluciones:N2} · anulaciones Bs {summary.Anulaciones:N2}; se muestran separadas de los cargos.";
    }
}
