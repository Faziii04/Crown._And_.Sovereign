using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Tablero;

public partial class VistaRapidaHabitacionControl : UserControl
{
    private HabitacionOperacionModel? _room;
    private string _action = "Detalles";
    public event Action<HabitacionOperacionModel, string>? AccionSolicitada;
    public event Action<HabitacionOperacionModel>? DetallesSolicitados;
    public VistaRapidaHabitacionControl() => InitializeComponent();
    public void Mostrar(HabitacionOperacionModel room)
    {
        _room = room; TitleText.Text = $"Habitación {room.Habitacion.Numero}"; SubtitleText.Text = $"{room.Habitacion.Tipo} · Piso {room.Habitacion.Piso} · capacidad {room.Habitacion.Capacidad}";
        ClassificationText.Text = room.Clasificacion; GuestText.Text = room.Huespedes.Count > 0 ? string.Join(" · ", room.Huespedes.Select(item => item.Nombre)) : room.ProximaReserva?.Huesped ?? "Sin huésped actualmente";
        DatesText.Text = room.ReservaActual is not null ? $"{room.ReservaActual.Llegada:dd MMM} – {room.ReservaActual.Salida:dd MMM} · Reserva #{room.ReservaActual.Id}" : room.ProximaReserva is not null ? $"Llegada {room.ProximaReserva.Llegada:dd MMM} · salida {room.ProximaReserva.Salida:dd MMM} · Reserva #{room.ProximaReserva.Id}" : "Sin estancia o reserva próxima";
        ConditionsText.Text = $"Limpieza: {room.EstadoLimpieza}\nMantenimiento: {room.EstadoMantenimiento}";
        NextText.Text = room.Incidencias.FirstOrDefault()?.Descripcion ?? room.ProximaFecha;
        (_action, ActionButton.Content) = HabitacionCardAcciones.Principal(room);
    }
    private void Action_Click(object sender, RoutedEventArgs e) { if (_room is not null) AccionSolicitada?.Invoke(_room, _action); }
    private void Details_Click(object sender, RoutedEventArgs e) { if (_room is not null) DetallesSolicitados?.Invoke(_room); }
}

public static class HabitacionCardAcciones
{
    public static (string Action, string Label) Principal(HabitacionOperacionModel room)
    {
        if (room.Bloqueada && room.Ocupada) return ("Cambiar habitación", "Cambiar habitación");
        if (room.Ocupada && room.SalidaHoy) return ("Checkout", "Ver salida");
        if (room.Ocupada) return ("Estancia", "Ver estancia");
        if (room.ProximaReserva is not null && room.LlegadaHoy) return (room.Clasificacion == "Lista para ingreso" ? "Check-in" : "Reserva", room.Clasificacion == "Lista para ingreso" ? "Realizar check-in" : "Ver reserva");
        if (room.Clasificacion == "Bloqueada") return ("Incidencias", "Ver incidencia");
        if (room.Clasificacion == "Por preparar") return ("Preparación", "Ver preparación");
        if (room.ProximaReserva is not null) return ("Reserva", "Ver reserva");
        return ("Crear reserva", "Crear reserva");
    }
}
