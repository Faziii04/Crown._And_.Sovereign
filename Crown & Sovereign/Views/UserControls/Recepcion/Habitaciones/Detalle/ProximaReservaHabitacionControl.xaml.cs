using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Detalle;

public partial class ProximaReservaHabitacionControl : UserControl
{
    private int? _reservationId;
    public event Action<int>? ReservaSolicitada;
    public ProximaReservaHabitacionControl() => InitializeComponent();
    public void Mostrar(HabitacionOperacionModel room)
    {
        var reservation = room.ProximaReserva; _reservationId = reservation?.Id;
        if (reservation is null) { GuestText.Text = "Sin próxima reserva"; ReservationText.Text = "No hay llegadas asignadas a esta habitación."; ReadinessText.Text = room.Clasificacion == "Lista para ingreso" ? "La habitación está lista para nuevas solicitudes." : $"Condición actual: {room.Clasificacion} · {room.EstadoLimpieza}"; OpenButton.Visibility = Visibility.Collapsed; return; }
        GuestText.Text = reservation.Huesped; ReservationText.Text = $"Reserva #{reservation.Id} · {reservation.Llegada:dd MMM yyyy} – {reservation.Salida:dd MMM yyyy}\n{reservation.Personas} huésped(es) · {reservation.Paquete}";
        ReadinessText.Text = $"Preparación: {room.EstadoLimpieza} · {room.Clasificacion}"; OpenButton.Visibility = Visibility.Visible;
    }
    private void Open_Click(object sender, RoutedEventArgs e) { if (_reservationId is int id) ReservaSolicitada?.Invoke(id); }
}
