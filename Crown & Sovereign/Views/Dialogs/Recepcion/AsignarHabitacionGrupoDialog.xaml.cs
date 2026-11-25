using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion;

public partial class AsignarHabitacionGrupoDialog : Window
{
    private readonly GrupoReservaModel _group;
    private readonly GrupoReservaService _service = new();
    public AsignarHabitacionGrupoDialog(GrupoReservaModel group)
    {
        InitializeComponent(); _group = group; GroupText.Text = group.Nombre;
        ReservationBox.ItemsSource = SampleData.Reservas.Where(item => item.GrupoId == group.Id && item.Estado is not ("Cancelada" or "Finalizada")).ToList();
        ReservationBox.SelectedIndex = 0;
    }
    private void Reservation_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ReservationBox.SelectedItem is not ReservaModel reservation) { RoomBox.ItemsSource = null; InfoText.Text = "Crea primero una reserva para este grupo."; return; }
        var criteria = new ReservationSearchCriteria { Llegada = reservation.Llegada, Salida = reservation.Salida, Personas = Math.Max(1, reservation.Personas), Habitaciones = 1 };
        var assigned = _service.HabitacionesAsignadas(reservation.Id).Select(item => item.Id).ToHashSet();
        var rooms = new HabitacionDisponibilidadService().BuscarHabitaciones(criteria, excluirReservaId: reservation.Id).Where(item => !assigned.Contains(item.Id)).ToList();
        RoomBox.ItemsSource = rooms; RoomBox.DisplayMemberPath = "Numero"; RoomBox.SelectedItem = rooms.FirstOrDefault();
        InfoText.Text = rooms.Count == 0 ? "No hay habitaciones libres para toda la estancia de esta reserva." : $"{reservation.Llegada:dd/MM/yyyy} – {reservation.Salida:dd/MM/yyyy} · {rooms.Count} opciones disponibles. La disponibilidad se validará de nuevo al añadir.";
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try { if (ReservationBox.SelectedItem is not ReservaModel reservation || RoomBox.SelectedItem is not HabitacionDisponibleModel room) throw new InvalidOperationException("Selecciona una reserva y una habitación disponible."); _service.AsignarHabitacion(reservation.Id, room.Id); DialogResult = true; }
        catch (Exception ex) { InfoText.Text = ex.Message; }
    }
}
