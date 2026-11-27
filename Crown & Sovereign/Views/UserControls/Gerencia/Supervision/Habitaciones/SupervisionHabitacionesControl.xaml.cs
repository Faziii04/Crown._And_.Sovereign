using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision;

public partial class SupervisionHabitacionesControl : UserControl
{
    private readonly SupervisionOperativaService _service = new();
    private SupervisionSnapshot? _snapshot;
    private IReadOnlyList<SupervisionHabitacionFila> _allRooms = [];

    public SupervisionHabitacionesControl() => InitializeComponent();

    public void Cargar(SupervisionSnapshot snapshot)
    {
        _snapshot = snapshot; _allRooms = snapshot.Habitaciones;
        var selectedFloor = (FloorBox.SelectedItem as ComboBoxItem)?.Tag;
        FloorBox.Items.Clear(); FloorBox.Items.Add(new ComboBoxItem { Content = "Todos los pisos", IsSelected = true });
        foreach (var floor in _allRooms.Select(item => item.Piso).Distinct().OrderBy(item => item)) FloorBox.Items.Add(new ComboBoxItem { Content = $"Piso {floor}", Tag = floor });
        if (selectedFloor is int id) FloorBox.SelectedItem = FloorBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Tag is int floor && floor == id) ?? FloorBox.Items[0];
        else FloorBox.SelectedIndex = 0;
        ArrivalsGrid.ItemsSource = snapshot.Ingresos;
        ApplyFilters();
    }

    public void AbrirAlerta(SupervisionAlerta alert)
    {
        if (alert.Entidad.StartsWith("Reserva #", StringComparison.Ordinal) && int.TryParse(alert.Entidad.AsSpan("Reserva #".Length), out var reservationId))
        {
            RoomTabs.SelectedIndex = 2;
            ArrivalsGrid.SelectedItem = _snapshot?.Ingresos.FirstOrDefault(item => item.ReservaId == reservationId);
            ArrivalsGrid.ScrollIntoView(ArrivalsGrid.SelectedItem);
            return;
        }
        var room = _allRooms.FirstOrDefault(item => item.Id == alert.RegistroId);
        if (room is null) return;
        RoomTabs.SelectedIndex = 1; RoomGrid.SelectedItem = room; RoomGrid.ScrollIntoView(room); ShowRoom(room);
    }

    private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilters();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    private void ApplyFilters()
    {
        if (RoomGrid is null) return;
        var search = SearchBox.Text.Trim();
        var floor = (FloorBox.SelectedItem as ComboBoxItem)?.Tag as int?;
        var state = (StateBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos los estados";
        var rows = _allRooms.Where(item => !floor.HasValue || item.Piso == floor.Value)
            .Where(item => state == "Todos los estados" || item.Estado == state)
            .Where(item => string.IsNullOrWhiteSpace(search) || $"{item.Numero} {item.Tipo} {item.Estado} {item.Ocupacion} {item.Limpieza} {item.NotaAtencion}".Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        BoardList.ItemsSource = rows; RoomGrid.ItemsSource = rows;
        if (DetailTitle.Text.Length == 0) RoomGrid.SelectedIndex = rows.Count > 0 ? 0 : -1;
    }

    private void SelectRoom_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is SupervisionHabitacionFila room) ShowRoom(room);
    }
    private void Room_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RoomGrid.SelectedItem is SupervisionHabitacionFila room) ShowRoom(room);
    }
    private void ShowArrivals_Click(object sender, RoutedEventArgs e) => RoomTabs.SelectedIndex = 2;
    private void Arrival_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ArrivalsGrid.SelectedItem is not SupervisionIngresoFila arrival) return;
        DetailTitle.Text = $"Reserva #{arrival.ReservaId} · {arrival.Huesped}";
        DetailText.Text = $"Llegada: {arrival.Llegada:dd/MM/yyyy}\nHabitación(es): {arrival.Habitaciones}\nPreparación: {arrival.Condicion}\nCondición: {(arrival.Condicion == "Preparada" ? "Preparada" : arrival.Prioridad)}\n\nLas fechas de la reserva no contienen una hora de llegada individual.";
        RoomIncidents.ItemsSource = null; AlternativesList.ItemsSource = null;
    }
    private void ShowRoom(SupervisionHabitacionFila room)
    {
        DetailTitle.Text = $"Habitación {room.Numero} · {room.Tipo}";
        DetailText.Text = $"Piso {room.Piso}\nEstado principal: {room.Estado}\nOcupación: {room.Ocupacion}\nHuésped(es): {room.HuespedesActuales}\nReserva actual: {room.ReservaActual}\nLimpieza: {room.Limpieza}\nBloqueo activo: {(room.BloqueoActivo ? "Sí" : "No")}\nPróxima llegada: {room.ProximaLlegada}\nPróxima salida: {room.ProximaSalida}\nPreparación: {room.Preparacion}\nAtención: {room.NotaAtencion}";
        RoomIncidents.ItemsSource = _snapshot?.Incidencias.Where(item => item.HabitacionId == room.Id).ToList();
        AlternativesList.ItemsSource = room.ReservaProximaId is int reservationId
            ? _service.ObtenerAlternativas(room.Id, reservationId).Select(item => $"Hab. {item.Numero} · Piso {item.Piso} · {item.Tipo} · {item.Estado}").ToList()
            : new[] { "No hay próxima reserva asignada para consultar alternativas." };
        RoomBasisText.Text = "Las alternativas consideran capacidad registrada, preparación actual, bloqueo actual y disponibilidad de la habitación para todo el intervalo de reserva. No realizan cambios.";
    }
}


