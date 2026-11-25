using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones;

public partial class DetalleHabitacionControl : UserControl
{
    private HabitacionModel? _habitacion;
    public event Action<HabitacionModel>? ReservarSolicitada;
    public event Action<HabitacionModel>? EditarSolicitado;
    public DetalleHabitacionControl() => InitializeComponent();
    public void Mostrar(HabitacionModel room, bool administracion)
    {
        _habitacion = room;
        RoomTitle.Text = $"Habitación {room.Numero}";
        RoomSubtitle.Text = $"Piso {room.Piso} · {room.Tipo}";
        OccupancyText.Text = room.Estado;
        GuestText.Text = room.Huesped ?? "Sin huésped asignado";
        CleaningText.Text = room.Limpieza;
        MaintenanceText.Text = room.Bloqueada ? "Fuera de servicio" : "Operativa";
        IncidentText.Text = SampleData.Incidencias.FirstOrDefault(i => i.Habitacion == room.Id && i.Estado != "Resuelta")?.Descripcion ?? "Sin incidencias abiertas";
        CapacityText.Text = $"Capacidad máxima: {room.Capacidad} huéspedes";
        AmenitiesText.Text = room.Comodidades;
        EditButton.Visibility = administracion ? Visibility.Visible : Visibility.Collapsed;
        ReservationButton.Visibility = administracion ? Visibility.Collapsed : Visibility.Visible;
    }
    private void Reservation_Click(object sender, RoutedEventArgs e) { if (_habitacion is not null) ReservarSolicitada?.Invoke(_habitacion); }
    private void Edit_Click(object sender, RoutedEventArgs e) { if (_habitacion is not null) EditarSolicitado?.Invoke(_habitacion); }
    private void CleaningRequest_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Solicitud enviada al equipo de limpieza.", "Habitación");
    private void Incident_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Incidencia remitida a Mantenimiento.", "Habitación");
}
