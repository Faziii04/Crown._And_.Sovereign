using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class FolioControl : UserControl
{
    private int _stayId;
    private readonly FolioService _folio = new();
    public FolioControl() => InitializeComponent();
    public void SetReadOnly(bool readOnly) { PaymentButton.IsEnabled = !readOnly; ChargeButton.IsEnabled = !readOnly; }
    public void Mostrar(int stayId)
    {
        _stayId = stayId;
        var stay = SampleData.Estancias.First(item => item.Id == stayId);
        var folioId = stay.FolioId ?? SampleData.Folios.First(item => item.EstanciaId == stayId).Id;
        var charges = SampleData.Cargos.Where(item => item.FolioId == folioId && !item.Anulado).Sum(item => item.Monto);
        var paid = SampleData.Pagos.Where(item => item.FolioId == folioId && item.Estado != "Anulado").Sum(item => item.Monto);
        DataContext = new { Movimientos = _folio.ConsultarMovimientosPorEstancia(stayId) };
        ChargesText.Text = $"Bs {charges:N2}"; PaidText.Text = $"Bs {paid:N2}"; BalanceText.Text = $"Bs {_folio.CalcularSaldoFolio(folioId):N2}";
        var unassigned = _folio.ContarMovimientosSinEstancia(stayId);
        LegacyNotice.Text = unassigned == 0 ? "" : $"Hay {unassigned} movimiento(s) anteriores vinculados solo al huésped. No se suman a esta estancia porque no tienen un ID de estancia; revisa su atribución antes de cerrar la cuenta.";
        LegacyNotice.Visibility = unassigned == 0 ? Visibility.Collapsed : Visibility.Visible;
    }
    private void Payment_Click(object sender, RoutedEventArgs e)
    {
        var stay = SampleData.Estancias.First(item => item.Id == _stayId);
        var folioId = stay.FolioId ?? SampleData.Folios.First(item => item.EstanciaId == _stayId).Id;
        var dialog = new PagoDialog(folioId) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) Mostrar(_stayId);
    }
    private void Charge_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CargoDialog(_stayId, true) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) Mostrar(_stayId);
    }
}
