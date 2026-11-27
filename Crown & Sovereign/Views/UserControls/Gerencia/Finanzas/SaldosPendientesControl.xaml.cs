using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Finanzas;

public partial class SaldosPendientesControl : UserControl
{
    private readonly FinancialManagementService _service = new();
    public event EventHandler<int>? MovementsRequested;
    public SaldosPendientesControl()
    {
        InitializeComponent();
        StateBox.ItemsSource = new[] { "Abiertos", "Con saldo", "Crédito a favor", "Todos" };
        StateBox.SelectedIndex = 0;
    }
    public void Actualizar() => Recargar();
    private void Filter_Changed(object sender, TextChangedEventArgs e) => Recargar();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => Recargar();
    private void Recargar()
    {
        if (BalanceGrid is null || StateBox.SelectedItem is not string state) return;
        BalanceGrid.ItemsSource = _service.Saldos(SearchBox.Text.Trim(), state);
    }
    private void Balance_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BalanceGrid.SelectedItem is not FinancialBalanceRow row) { SelectionText.Text = "Selecciona un folio para ver su contexto."; MovementsButton.IsEnabled = false; return; }
        MovementsButton.IsEnabled = true;
        var label = row.Balance < 0 ? "Crédito a favor" : row.Balance == 0 ? "Sin saldo pendiente" : "Saldo pendiente";
        SelectionText.Text = $"Folio #{row.FolioId} · {row.Guest} · {row.Context} · {row.Type}. {label}: {FinanzasControl.Money(Math.Abs(row.Balance))}. Cargos y cobros se consultan directamente desde movimientos; no se agrupan folios maestros porque la relación padre-hijo no está disponible en el modelo local.";
    }
    private void OpenMovements_Click(object sender, System.Windows.RoutedEventArgs e)
    { if (BalanceGrid.SelectedItem is FinancialBalanceRow row) MovementsRequested?.Invoke(this, row.FolioId); }
}
