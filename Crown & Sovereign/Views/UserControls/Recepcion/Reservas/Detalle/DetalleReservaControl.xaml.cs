using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Detalle;

public partial class DetalleReservaControl : UserControl
{
    private readonly EstanciaService _stays = new();
    private readonly ReservaService _reservations = new();
    private ReservaModel? _reservation;
    public event Action? VolverSolicitado;
    public event Action? DatosActualizados;
    public DetalleReservaControl() => InitializeComponent();
    public void Cargar(int id)
    {
        _reservation = SampleData.Reservas.FirstOrDefault(item => item.Id == id);
        if (_reservation is null) return;
        TitleText.Text = $"Reserva #{id} · {_reservation.Huesped}";
        SubtitleText.Text = $"{_reservation.Llegada:dd MMM yyyy} – {_reservation.Salida:dd MMM yyyy} · {_reservation.Canal} · {_reservation.Estado}";
        SummarySection.Cargar(_reservation); RoomsSection.Cargar(_reservation); GuestsSection.Cargar(_reservation);
        ConfirmButton.Visibility = _reservation.Estado == "Pendiente" ? Visibility.Visible : Visibility.Collapsed;
        CheckInButton.Visibility = _reservation.Estado == "Confirmada" ? Visibility.Visible : Visibility.Collapsed;
        CheckOutButton.Visibility = _reservation.Estado == "En estancia" ? Visibility.Visible : Visibility.Collapsed;
        EditButton.Visibility = _reservation.Estado is "Cancelada" or "Finalizada" ? Visibility.Collapsed : Visibility.Visible;
        CancelButton.Visibility = _reservation.Estado is "Cancelada" or "Finalizada" or "En estancia" ? Visibility.Collapsed : Visibility.Visible;
    }
    private void Back_Click(object sender, RoutedEventArgs e) => VolverSolicitado?.Invoke();
    private void Confirm_Click(object sender, RoutedEventArgs e) => Ejecutar(() => _stays.ConfirmarReserva(_reservation!.Id));
    private void CheckIn_Click(object sender, RoutedEventArgs e) => Ejecutar(() => _stays.RealizarCheckIn(_reservation!.Id));
    private void CheckOut_Click(object sender, RoutedEventArgs e)
    {
        if (_reservation is null) return;
        var guestId = SampleData.ReservaHuespedes.FirstOrDefault(item => item.ReservaId == _reservation.Id && item.Principal)?.HuespedId ?? SampleData.Huespedes.FirstOrDefault(item => item.Nombre == _reservation.Huesped)?.Id;
        if (guestId is not int id) { MessageBox.Show("No se encontró el huésped principal.", "Check-out", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var dialog = new CheckOutDialog(id) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) Refrescar();
    }
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (_reservation is null) return;
        var dialog = new ReservaDialog(existente: _reservation) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) Refrescar();
    }
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (_reservation is null || MessageBox.Show(Window.GetWindow(this), "¿Cancelar esta reserva?", "Confirmar cancelación", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        Ejecutar(() => _reservations.Cancelar(_reservation));
    }
    private void Ejecutar(Action operation)
    {
        try { operation(); Refrescar(); }
        catch (Exception ex) { MessageBox.Show(Window.GetWindow(this), ex.Message, "No se pudo completar la acción", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void Refrescar() { if (_reservation is null) return; Cargar(_reservation.Id); DatosActualizados?.Invoke(); }
}
