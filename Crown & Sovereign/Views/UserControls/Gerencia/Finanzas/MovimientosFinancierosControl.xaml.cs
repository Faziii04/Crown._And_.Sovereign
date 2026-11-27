using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Finanzas;

public partial class MovimientosFinancierosControl : UserControl
{
    private readonly FinancialManagementService _service = new();
    private FinancialPeriod _period = new("Este mes", DateTime.Today, DateTime.Today.AddDays(1), DateTime.Today, DateTime.Today.AddDays(1));
    public MovimientosFinancierosControl()
    {
        InitializeComponent();
        KindBox.ItemsSource = new[] { "Todos", "Cargo", "Cobro", "Devolución", "Anulación" };
        KindBox.SelectedIndex = 0;
    }
    public void Actualizar(FinancialPeriod period) { _period = period; Recargar(); }
    public void BuscarFolio(int folioId) { SearchBox.Text = folioId.ToString(); KindBox.SelectedIndex = 0; Recargar(); }
    private void Filter_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e) => Recargar();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => Recargar();
    private void Recargar()
    {
        if (MovementGrid is null || KindBox.SelectedItem is not string kind) return;
        MovementGrid.ItemsSource = _service.Movimientos(_period, kind, SearchBox.Text.Trim());
    }
}
