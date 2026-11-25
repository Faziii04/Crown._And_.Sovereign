using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Detalle;

public partial class DisponibilidadHabitacionControl : UserControl
{
    private readonly ReservaService _reservations = new();
    private readonly HabitacionService _rooms = new();
    private HabitacionOperacionModel? _room;
    private DateTime _start = DateTime.Today;
    private int _days = 14;
    private bool _initializing;
    public event Action? DatosActualizados;
    public event Action<int>? ReservaSolicitada;
    public event Action<HabitacionOperacionModel, DateTime, int>? CalendarioGeneralSolicitado;
    public DisponibilidadHabitacionControl()
    {
        InitializeComponent(); HorizonBox.ItemsSource = new[] { 7, 14, 30 }; HorizonBox.SelectedItem = 14;
    }
    public void Mostrar(HabitacionOperacionModel room)
    {
        _room = room; TitleText.Text = $"Disponibilidad de la habitación {room.Habitacion.Numero}";
        _initializing = true; StartPicker.SelectedDate = _start; EndPicker.SelectedDate = _start.AddDays(_days); _initializing = false;
        RefreshTimeline();
    }
    private void RefreshTimeline()
    {
        if (_room is null) return;
        var rows = _reservations.ObtenerCalendario(_start, _days, piso: _room.Habitacion.Piso, busquedaHabitacion: _room.Habitacion.Numero);
        var timeline = rows.FirstOrDefault(item => item.RoomId == _room.Habitacion.Id);
        if (timeline is null) return;
        DateHeader.ItemsSource = timeline.Days; DayCells.ItemsSource = timeline.Days; ReservationBars.ItemsSource = timeline.Bars;
        TimelineGrid.Width = timeline.CalendarWidth;
        BlockBar.Width = Math.Max(110, timeline.CalendarWidth);
        var blockedIncidents = _room.Incidencias.Where(item => item.BloqueaHabitacion).ToList();
        BlockBar.Visibility = _room.Bloqueada || blockedIncidents.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        NoBlockText.Visibility = BlockBar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        BlockText.Text = blockedIncidents.FirstOrDefault()?.Descripcion ?? "Bloqueo activo · no hay fecha final registrada";
        var start = StartPicker.SelectedDate ?? _start; var end = EndPicker.SelectedDate ?? start.AddDays(_days);
        SelectionText.Text = $"Período seleccionado: {start:dd/MM/yyyy} – {end:dd/MM/yyyy} · salida excluida del cómputo de noches.";
    }
    private void Previous_Click(object sender, RoutedEventArgs e) { _start = _start.AddDays(-_days); SyncDates(); RefreshTimeline(); }
    private void Next_Click(object sender, RoutedEventArgs e) { _start = _start.AddDays(_days); SyncDates(); RefreshTimeline(); }
    private void Today_Click(object sender, RoutedEventArgs e) { _start = DateTime.Today; SyncDates(); RefreshTimeline(); }
    private void Horizon_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (HorizonBox.SelectedItem is not int days) return;
        _days = days; if (!_initializing) { EndPicker.SelectedDate = (StartPicker.SelectedDate ?? _start).AddDays(_days); RefreshTimeline(); }
    }
    private void Period_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || !IsLoaded) return;
        if (StartPicker.SelectedDate is DateTime start) _start = start.Date;
        RefreshTimeline();
    }
    private void SyncDates()
    {
        _initializing = true; StartPicker.SelectedDate = _start; EndPicker.SelectedDate = _start.AddDays(_days); _initializing = false;
    }
    private void Day_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_room is null || (sender as FrameworkElement)?.DataContext is not ReservationTimelineCell cell || cell.Status != "Libre") return;
        StartPicker.SelectedDate = cell.Date.Date; EndPicker.SelectedDate = cell.Date.Date.AddDays(1); SelectionText.Text = $"Noche libre seleccionada: {cell.Date:dd/MM/yyyy}. Puedes ampliar el período antes de crear la reserva.";
    }
    private void Reservation_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ReservationBarModel bar) ReservaSolicitada?.Invoke(bar.ReservaId);
    }
    private void CreateReservation_Click(object sender, RoutedEventArgs e)
    {
        if (_room is null) return;
        var start = StartPicker.SelectedDate ?? DateTime.MinValue; var end = EndPicker.SelectedDate ?? DateTime.MinValue;
        if (end <= start) { MessageBox.Show(Window.GetWindow(this), "La salida debe ser posterior a la llegada.", "Período inválido", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (!_rooms.ConsultarDisponibilidad(_room.Habitacion.Id, start, end)) { MessageBox.Show(Window.GetWindow(this), "La habitación no está disponible durante todo el período seleccionado.", "Disponibilidad", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var draft = new BorradorReserva { Llegada = start.Date, Salida = end.Date, Personas = 1, HabitacionesRequeridas = 1, TipoPreferido = _room.Habitacion.Tipo, PasoInicial = 0 };
        draft.HabitacionesSeleccionadas.Add(_room.Habitacion.Id);
        var dialog = new ReservaDialog(borrador: draft) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) DatosActualizados?.Invoke();
    }
    private void GeneralCalendar_Click(object sender, RoutedEventArgs e)
    {
        if (_room is not null) CalendarioGeneralSolicitado?.Invoke(_room, StartPicker.SelectedDate ?? _start, _days);
    }
}
