using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class DetalleReservaDialog : Window
{
    private readonly ReservaService _service = new();
    public ReservaModel Reserva { get; }
    public DetalleReservaDialog(ReservaModel reserva)
    {
        InitializeComponent();
        Reserva = reserva;
        TitleText.Text = $"Reserva #{reserva.Id}";
        GuestText.Text = reserva.Huesped;
        var room = SampleData.Habitaciones.First(h => h.Id == reserva.Habitacion);
        RoomText.Text = $"Habitación {room.Numero} · {room.Tipo} · Piso {room.Piso}";
        DatesText.Text = $"{reserva.Llegada:dddd, dd MMMM yyyy} – {reserva.Salida:dddd, dd MMMM yyyy} · {(reserva.Salida.Date - reserva.Llegada.Date).Days} noches";
        ChannelText.Text = $"Canal: {reserva.Canal}";
        PackageText.Text = $"Paquete: {reserva.Paquete}";
        StatusText.Text = $"Estado: {reserva.Estado}";
        CancelReservationButton.IsEnabled = reserva.Estado != "Cancelada";
        if (reserva.Estado == "Cancelada") CancelReservationButton.Visibility = Visibility.Collapsed;
    }
    private void CancelReservation_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("¿Cancelar esta reserva?", "Confirmar cancelación", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        _service.Cancelar(Reserva);
        DialogResult = true;
    }
    private void EditReservation_Click(object sender, RoutedEventArgs e)
    {
        var edit = new ReservaDialog(Reserva.Llegada, SampleData.Habitaciones.First(h => h.Id == Reserva.Habitacion).Tipo, Reserva) { Owner = this };
        if (edit.ShowDialog() == true) DialogResult = true;
    }
}
