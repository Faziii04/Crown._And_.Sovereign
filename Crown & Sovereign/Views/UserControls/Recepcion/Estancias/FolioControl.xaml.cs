using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class FolioControl : UserControl
{
    private int _stayId;
    private readonly FolioService _folio = new();
    public event EventHandler? DatosActualizados;
    public FolioControl() => InitializeComponent();
    private IReadOnlyList<FolioLine> _movements = [];
    private bool _readOnly;
    public void SetReadOnly(bool readOnly) { _readOnly = readOnly; RefreshActions(); }
    public void Mostrar(int stayId)
    {
        _stayId = stayId;
        var stay = SampleData.Estancias.First(item => item.Id == stayId);
        var folio = stay.FolioId is int id ? SampleData.Folios.FirstOrDefault(item => item.Id == id) : SampleData.Folios.FirstOrDefault(item => item.EstanciaId == stayId);
        var folioId = folio?.Id;
        FolioLabel.Text = folio is null ? $"Estancia #{stay.Id} · sin folio asociado" : $"Folio #{folio.Id} · Estancia #{stay.Id} · Reserva #{stay.ReservaId}";
        FolioStateText.Text = folio?.Estado ?? "Sin folio";
        if (MovementFilter.SelectedIndex < 0) MovementFilter.SelectedIndex = 0;
        _movements = folioId is int currentId ? _folio.ConsultarMovimientosPorFolio(currentId) : [];
        var charges = folioId is int current ? SampleData.Cargos.Where(item => item.FolioId == current && !item.Anulado).Sum(item => item.Monto) : 0m;
        var paid = folioId is int paidFolio ? SampleData.Pagos.Where(item => item.FolioId == paidFolio && item.Estado != "Anulado").Sum(item => item.Monto) : 0m;
        ChargesText.Text = $"Bs {charges:N2}"; PaidText.Text = $"Bs {paid:N2}";
        BalanceText.Text = $"Bs {new FolioService().CalcularSaldoPorEstancia(stayId):N2}";
        var rooms = new EstanciaService().ObtenerHabitacionesEstancia(stayId);
        var reservation = SampleData.Reservas.First(item => item.Id == stay.ReservaId);
        var remainingNights = Math.Max(0, (stay.CheckOut.Date - DateTime.Today).Days);
        var futureEstimate = remainingNights > 0
            ? rooms.Sum(room => new TarifaService().CalcularAlojamiento(reservation, room, DateTime.Today, stay.CheckOut.Date))
            : 0m;
        FutureLodgingText.Text = remainingNights == 0
            ? "Sin noches futuras estimadas. Esta estimación no modifica el folio."
            : $"{remainingNights} noche(s) · Bs {futureEstimate:N2}. Estimación según precios acordados; no representa un cargo confirmado.";
        ApplyFilter();
        RefreshActions();
        var unassigned = _folio.ContarMovimientosSinEstancia(stayId);
        LegacyNotice.Text = unassigned == 0 ? "" : $"Hay {unassigned} movimiento(s) anteriores vinculados solo al huésped. No se suman a esta estancia porque no tienen un ID de estancia; revisa su atribución antes de cerrar la cuenta.";
        LegacyNotice.Visibility = unassigned == 0 ? Visibility.Collapsed : Visibility.Visible;
    }
    private void RefreshActions()
    {
        var stay = SampleData.Estancias.FirstOrDefault(item => item.Id == _stayId);
        var folioId = stay?.FolioId ?? SampleData.Folios.FirstOrDefault(item => item.EstanciaId == _stayId)?.Id;
        var enabled = !_readOnly && folioId is int id && SampleData.Folios.FirstOrDefault(item => item.Id == id)?.Estado == "Abierto" && new FolioService().CalcularSaldoPorEstancia(_stayId) > 0;
        PaymentButton.IsEnabled = enabled;
    }
    private void Filter_Changed(object sender, RoutedEventArgs e) { if (IsLoaded) ApplyFilter(); }
    private void ApplyFilter()
    {
        var filter = (MovementFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
        var search = MovementSearch.Text?.Trim() ?? "";
        var rows = _movements.Where(item => filter switch
        {
            "Cargos" => item.Tipo == "Cargo",
            "Pagos" => item.Tipo == "Pago" || item.Tipo == "Anulación",
            "Correcciones" => item.Tipo is "Devolución" or "Anulación",
            _ => true
        }).Where(item => string.IsNullOrWhiteSpace(search) || $"{item.Fecha:dd/MM/yyyy HH:mm} {item.Tipo} {item.Concepto} {item.Origen} {item.Estado} {item.Importe}".Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        FolioGrid.ItemsSource = rows;
        EmptyText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (rows.Count == 0 && _movements.Count > 0) EmptyText.Text = "No hay movimientos que coincidan con la búsqueda.";
        else EmptyText.Text = "No existen movimientos registrados en este folio.";
    }
    private void Payment_Click(object sender, RoutedEventArgs e)
    {
        var stay = SampleData.Estancias.First(item => item.Id == _stayId);
        var folioId = stay.FolioId ?? SampleData.Folios.First(item => item.EstanciaId == _stayId).Id;
        var dialog = new PagoDialog(folioId) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { Mostrar(_stayId); DatosActualizados?.Invoke(this, EventArgs.Empty); }
    }
}
