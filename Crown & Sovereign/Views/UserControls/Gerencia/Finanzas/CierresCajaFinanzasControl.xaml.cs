using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Finanzas;

public partial class CierresCajaFinanzasControl : UserControl
{
    private readonly FinancialManagementService _service = new();
    public CierresCajaFinanzasControl()
    {
        InitializeComponent();
        StateBox.ItemsSource = new[] { "Todos", "Abiertos", "Cerrados" };
        StateBox.SelectedIndex = 0;
    }
    public void Actualizar() => Recargar();
    private void Filter_Changed(object sender, TextChangedEventArgs e) => Recargar();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => Recargar();
    private void Recargar()
    {
        if (ShiftGrid is null || StateBox.SelectedItem is not string state) return;
        var selectedId = (ShiftGrid.SelectedItem as FinancialCashShiftRow)?.Shift.Id;
        ShiftGrid.ItemsSource = _service.Turnos(SearchBox.Text.Trim(), state);
        ShiftGrid.SelectedItem = ShiftGrid.Items.Cast<FinancialCashShiftRow>().FirstOrDefault(item => item.Shift.Id == selectedId);
        if (ShiftGrid.SelectedItem is null) ShowDetails(null);
    }
    private void Shift_SelectionChanged(object sender, SelectionChangedEventArgs e) => ShowDetails(ShiftGrid.SelectedItem as FinancialCashShiftRow);
    private void ShowDetails(FinancialCashShiftRow? row)
    {
        if (row is null)
        {
            CashMovementsGrid.ItemsSource = Array.Empty<object>();
            SelectedTitle.Text = "Selecciona un turno para consultar el detalle.";
            SelectedDetails.Text = "Los cierres conservan el efectivo esperado, el importe contado y la diferencia declarada por el cajero.";
            ReviewButton.IsEnabled = false;
            return;
        }
        CashMovementsGrid.ItemsSource = new CajaService().ListarMovimientos(row.Shift.Id);
        SelectedTitle.Text = $"Turno #{row.Shift.Id} · {row.Cashier} · {row.State}";
        if (row.Closure is { } closure)
        {
            var reviewer = closure.UsuarioRevisorId is int reviewerId ? Crown___Sovereign.Data.SampleData.Usuarios.FirstOrDefault(item => item.Id == reviewerId)?.Nombre ?? "—" : "Sin revisar";
            SelectedDetails.Text = $"Apertura {row.Opened:g} · cierre {closure.Fecha:g}. Fondo inicial {FinanzasControl.Money(closure.AperturaMonto)} + cobros en efectivo {FinanzasControl.Money(closure.EfectivoCobros)} − devoluciones en efectivo {FinanzasControl.Money(closure.EfectivoDevoluciones)} − anulaciones en efectivo {FinanzasControl.Money(row.CashCancellations)} = esperado {FinanzasControl.Money(closure.Esperado)}; contado {FinanzasControl.Money(closure.Contado)}; diferencia preservada {FinanzasControl.Money(closure.Diferencia)}. Cajero anotó: {closure.Observaciones}. Revisión gerencial: {reviewer}{(closure.FechaRevision is DateTime reviewed ? $" · {reviewed:g}" : "")}.";
            ReviewButton.IsEnabled = closure.FechaRevision is null;
        }
        else
        {
            SelectedDetails.Text = $"Turno abierto desde {row.Opened:g}. Fondo inicial {FinanzasControl.Money(row.Shift.MontoInicial)}. Efectivo esperado ahora {FinanzasControl.Money(row.CashExpected)}. Cobros no efectivo asociados {FinanzasControl.Money(row.NonCashCollections)}. Este módulo permite supervisar; el cierre lo registra el cajero en Caja.";
            ReviewButton.IsEnabled = false;
        }
    }
    private void Review_Click(object sender, RoutedEventArgs e)
    {
        if (ShiftGrid.SelectedItem is not FinancialCashShiftRow { Closure: { } closure } row || closure.FechaRevision is not null) return;
        var result = MessageBox.Show($"Registrar la revisión gerencial del cierre #{closure.Id}?\n\nLa diferencia de {FinanzasControl.Money(closure.Diferencia)} y la observación original del cajero se conservarán.", "Revisar cierre", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;
        try { _service.RevisarCierre(closure.Id); Recargar(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo revisar", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
