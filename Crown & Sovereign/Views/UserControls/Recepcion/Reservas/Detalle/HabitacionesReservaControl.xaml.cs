using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Detalle;

public partial class HabitacionesReservaControl : UserControl
{
    public HabitacionesReservaControl() => InitializeComponent();
    public void Cargar(ReservaModel reservation)
    {
        var assignments = SampleData.ReservaHabitaciones.Where(item => item.ReservaId == reservation.Id).ToList();
        var roomIds = assignments.Select(item => item.HabitacionId).Append(reservation.Habitacion).Distinct().ToList();
        RoomsGrid.ItemsSource = roomIds.Select(roomId =>
        {
            var room = SampleData.Habitaciones.First(item => item.Id == roomId);
            var assignmentId = assignments.FirstOrDefault(item => item.HabitacionId == roomId)?.ReservaHabitacionId;
            var guests = SampleData.ReservaHabitacionHuespedes.Where(item => item.ReservaId == reservation.Id && item.ReservaHabitacionId == assignmentId).Select(item => SampleData.Huespedes.FirstOrDefault(guest => guest.Id == item.HuespedId)?.Nombre).Where(name => name is not null);
            return new { room.Numero, room.Tipo, room.Piso, room.Limpieza, Bloqueo = room.Bloqueada ? "Bloqueada" : "Operativa", Huespedes = string.Join(", ", guests) };
        }).ToList();
    }
}
