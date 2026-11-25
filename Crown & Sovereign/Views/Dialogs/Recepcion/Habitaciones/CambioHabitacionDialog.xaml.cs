using System.Windows;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.Dialogs.Recepcion.Habitaciones;

public sealed record HabitacionDestinoOpcion(int Id, string RoomTitle, string Detail, string Availability, bool Seleccionada = false);

public partial class CambioHabitacionDialog : Window
{
    private readonly HabitacionOperacionModel _current;
    private readonly EstanciaService _stays = new();
    private IReadOnlyList<HabitacionOperacionModel> _candidates = [];
    private int? _selectedRoom;
    public CambioHabitacionDialog(HabitacionOperacionModel current)
    {
        InitializeComponent(); _current = current;
        var reservation = current.ReservaActual ?? throw new InvalidOperationException("La habitación no tiene una estancia asociada.");
        StayText.Text = $"Estancia #{current.EstanciaActual?.Id} · Reserva #{reservation.Id} · salida prevista {reservation.Salida:dd/MM/yyyy}";
        CurrentRoomText.Text = $"{current.Habitacion.Numero} · {current.Habitacion.Tipo} · piso {current.Habitacion.Piso}\n{current.Clasificacion} · {current.EstadoMantenimiento}\nSalida: {reservation.Salida:dd/MM/yyyy}";
        GuestsText.Text = $"Huéspedes afectados: {string.Join(", ", current.Huespedes.Select(item => item.Nombre))}";
        IncidentText.Text = current.Incidencias.FirstOrDefault()?.Descripcion ?? "Cambio solicitado por Operaciones.";
        FloorBox.ItemsSource = new[] { "Todos los pisos", "Piso 1", "Piso 2", "Piso 3", "Piso 4", "Piso 5" }; FloorBox.SelectedIndex = 0;
        TypeBox.ItemsSource = new[] { "Todos los tipos" }.Concat(SampleData.Tipos).ToList(); TypeBox.SelectedIndex = 0;
        AmenityBox.ItemsSource = new[] { "Todas las comodidades", "Wi-Fi", "Minibar", "Jacuzzi", "Balcón", "TV", "Aire acondicionado" }; AmenityBox.SelectedIndex = 0;
        ReasonBox.ItemsSource = new[] { "Incidencia de mantenimiento", "Reubicación operativa", "Solicitud del huésped", "Otro motivo" }; ReasonBox.SelectedIndex = 0;
        _candidates = _stays.HabitacionesCompatiblesParaCambio(reservation.Id, current.Habitacion.Id); RefreshCandidates();
    }
    private void RefreshCandidates()
    {
        var floor = int.TryParse(FloorBox.SelectedItem?.ToString()?.Replace("Piso ", ""), out var parsed) ? parsed : (int?)null;
        var type = TypeBox.SelectedItem?.ToString(); var amenity = AmenityBox.SelectedItem?.ToString();
        RoomList.ItemsSource = _candidates.Where(item => (floor is null || item.Habitacion.Piso == floor) && (type is "Todos los tipos" || item.Habitacion.Tipo == type) && (amenity is "Todas las comodidades" || item.Habitacion.Comodidades.Contains(amenity ?? "", StringComparison.OrdinalIgnoreCase)))
            .Select(item => new HabitacionDestinoOpcion(item.Habitacion.Id, $"{item.Habitacion.Numero} · {item.Habitacion.Tipo}", $"Piso {item.Habitacion.Piso} · {item.Habitacion.Capacidad} plazas", $"Lista · {item.EstadoLimpieza}", item.Habitacion.Id == _selectedRoom)).ToList();
        _selectedRoom = null;
    }
    private void Filter_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (IsLoaded) RefreshCandidates(); }
    private void SelectRoom_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not int id) return;
        _selectedRoom = id;
        RoomList.ItemsSource = (RoomList.ItemsSource as IEnumerable<HabitacionDestinoOpcion>)?.Select(item => item with { Seleccionada = item.Id == id }).ToList();
        ValidationText.Text = $"Habitación {SampleData.Habitaciones.First(item => item.Id == id).Numero} seleccionada.";
    }
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (_current.ReservaActual is null) return;
        if (_selectedRoom is not int id) { ValidationText.Text = "Selecciona una habitación destino."; return; }
        try { _stays.CambiarHabitacion(_current.ReservaActual.Id, _current.Habitacion.Id, id, ReasonBox.SelectedItem?.ToString() ?? "Otro motivo", NotesBox.Text); DialogResult = true; }
        catch (Exception ex) { ValidationText.Text = ex.Message; }
    }
}
