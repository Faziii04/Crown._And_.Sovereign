using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Detalle;

public partial class EstanciaActualHabitacionControl : UserControl
{
    private int? _guestId;
    public event Action<int>? EstanciaSolicitada;
    public EstanciaActualHabitacionControl() => InitializeComponent();
    public void Mostrar(HabitacionOperacionModel room)
    {
        _guestId = room.Huespedes.FirstOrDefault()?.Id;
        if (room.EstanciaActual is null) { GuestText.Text = "Sin estancia activa"; StayText.Text = "La habitación no tiene huéspedes alojados actualmente."; AssignmentText.Text = room.ProximaReserva is null ? "" : $"Próxima llegada: {room.ProximaReserva.Huesped} · {room.ProximaReserva.Llegada:dd MMM}"; OpenButton.Visibility = Visibility.Collapsed; return; }
        GuestText.Text = room.Huespedes.Count > 0 ? string.Join("\n", room.Huespedes.Select(item => item.Nombre)) : "No hay huéspedes asignados específicamente a esta habitación.";
        StayText.Text = $"Estancia #{room.EstanciaActual.Id} · Reserva #{room.EstanciaActual.ReservaId}\nEntrada: {room.EstanciaActual.CheckIn:dd MMM yyyy} · salida prevista: {room.EstanciaActual.CheckOut:dd MMM yyyy}";
        AssignmentText.Text = room.ReservaActual is not null ? $"Plan: {room.ReservaActual.Paquete} · {room.Huespedes.Count} huésped(es) asignados a esta habitación." : "Asignación individual no disponible.";
        OpenButton.Visibility = _guestId.HasValue ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Open_Click(object sender, RoutedEventArgs e) { if (_guestId is int id) EstanciaSolicitada?.Invoke(id); }
}
