using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Caja;

public partial class ResumenMediosPagoControl : UserControl
{
    public ResumenMediosPagoControl() => InitializeComponent();
    public void Mostrar(CashDeskShiftSummary summary)
    {
        var max = Math.Max(1m, summary.PorMetodo.Select(item => item.Importe).DefaultIfEmpty().Max());
        DataContext = new { Maximo = (double)max };
        MethodsList.ItemsSource = summary.PorMetodo;
        TotalText.Text = $"Bs {summary.TotalPagos:N2}";
    }
}
