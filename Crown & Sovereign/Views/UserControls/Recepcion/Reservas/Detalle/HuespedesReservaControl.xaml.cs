using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Detalle;

public partial class HuespedesReservaControl : UserControl
{
    public HuespedesReservaControl() => InitializeComponent();
    public void Cargar(ReservaModel reservation)
    {
        GuestsGrid.ItemsSource = SampleData.ReservaHuespedes.Where(link => link.ReservaId == reservation.Id).Select(link =>
        {
            var guest = SampleData.Huespedes.FirstOrDefault(item => item.Id == link.HuespedId);
            if (guest is null) return null;
            var assigned = SampleData.ReservaHabitacionHuespedes.FirstOrDefault(item => item.ReservaId == reservation.Id && item.HuespedId == guest.Id);
            var roomNumber = assigned is null ? "Sin asignar" : SampleData.Habitaciones.FirstOrDefault(item => item.Id == SampleData.ReservaHabitaciones.FirstOrDefault(roomLink => roomLink.ReservaHabitacionId == assigned.ReservaHabitacionId)?.HabitacionId)?.Numero ?? "Sin asignar";
            return new { Huesped = guest, Contacto = $"{guest.Telefono} · {guest.Email}", Rol = link.Principal ? "Principal" : "Acompañante", Habitacion = roomNumber };
        }).Where(item => item is not null).ToList();
    }
}
