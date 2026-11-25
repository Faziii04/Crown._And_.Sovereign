using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class AsignarHuespedGrupoDialog : Window
{
    private readonly int _reservationId;
    private readonly int _guestId;
    private readonly GrupoReservaService _service = new();
    public AsignarHuespedGrupoDialog(GrupoHuespedAsignacionModel guest)
    {
        InitializeComponent(); _reservationId = guest.ReservaId; _guestId = guest.Id;
        GuestText.Text = $"{guest.Nombre} · reserva #{guest.ReservaId}";
        var rooms = _service.HabitacionesAsignadas(guest.ReservaId); RoomBox.ItemsSource = rooms; RoomBox.SelectedItem = rooms.FirstOrDefault(item => item.Numero == guest.Habitacion) ?? rooms.FirstOrDefault();
        CapacityText.Text = rooms.Count == 0 ? "Primero añade habitaciones a esta reserva de grupo." : "La asignación respeta la capacidad de cada habitación.";
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try { if (RoomBox.SelectedItem is not HabitacionModel room) throw new InvalidOperationException("Esta reserva todavía no tiene habitaciones asignadas."); _service.MoverHuesped(_reservationId, room.Id, _guestId); DialogResult = true; }
        catch (Exception ex) { CapacityText.Text = ex.Message; }
    }
}
