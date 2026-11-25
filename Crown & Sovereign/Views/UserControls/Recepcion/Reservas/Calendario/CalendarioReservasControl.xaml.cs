using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Calendario;

public partial class CalendarioReservasControl : UserControl
{
    private readonly ReservaService _reservas = new();
    private int _dias = 7;
    public event Action? NuevaReservaSolicitada;
    public event Action<int>? ReservaSeleccionada;
    public event Action<int, DateTime>? HabitacionSeleccionada;

    public CalendarioReservasControl()
    {
        InitializeComponent();
        FloorFilter.ItemsSource = new[] { "Todos los pisos", "Piso 1", "Piso 2", "Piso 3", "Piso 4", "Piso 5" };
        TypeFilter.ItemsSource = new[] { "Todos los tipos" }.Concat(SampleData.Tipos).ToArray();
        StatusFilter.ItemsSource = new[] { "Todos los estados", "Confirmada", "Estancia activa", "Pendiente", "Mantenimiento", "Libre" };
        FloorFilter.SelectedIndex = TypeFilter.SelectedIndex = StatusFilter.SelectedIndex = 0;
        StartDatePicker.SelectedDate = DateTime.Today;
        Recargar();
    }

    public void Recargar()
    {
        var start = StartDatePicker.SelectedDate ?? DateTime.Today;
        var floorText = FloorFilter.SelectedItem?.ToString() ?? "Todos los pisos";
        var floor = int.TryParse(floorText.Replace("Piso ", ""), out var parsed) ? parsed : (int?)null;
        var type = TypeFilter.SelectedItem?.ToString();
        var status = StatusFilter.SelectedItem?.ToString();
        var days = Enumerable.Range(0, _dias).Select(i => { var date = start.AddDays(i); return new { Date = date, Label = date.ToString("ddd dd", new System.Globalization.CultureInfo("es-BO")) }; }).ToList();
        DataContext = new { Fechas = days, Habitaciones = _reservas.ObtenerCalendario(start, _dias, floor, type, status, RoomSearchBox?.Text ?? "", OnlyAvailableBox?.IsChecked == true) };
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Availability_Changed(object sender, RoutedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Horizon_Click(object sender, RoutedEventArgs e) { if (sender is Button { Tag: string tag } && int.TryParse(tag, out var days)) { _dias = days; Recargar(); } }
    private void NewReservation_Click(object sender, RoutedEventArgs e) => NuevaReservaSolicitada?.Invoke();
    private void ReservationCell_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || (sender as FrameworkElement)?.DataContext is not ReservationTimelineCell cell) return;
        if (cell.ReservationId is int reservationId) ReservaSeleccionada?.Invoke(reservationId);
        else HabitacionSeleccionada?.Invoke(cell.RoomId, cell.Date);
    }
    private void EmptyCell_Click(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || (sender as FrameworkElement)?.Tag is not ReservationTimelineCell cell || cell.ReservationId is not null) return;
        HabitacionSeleccionada?.Invoke(cell.RoomId, cell.Date);
    }
    private void ReservationBar_Click(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not ReservationBarModel bar) return;
        if (e.ClickCount >= 2) ReservaSeleccionada?.Invoke(bar.ReservaId);
        else { QuickPreviewText.Text = bar.Tooltip.Replace("\n", " · "); QuickPreviewText.Visibility = Visibility.Visible; }
        e.Handled = true;
    }
    private void ContextOpen_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is ReservationBarModel bar) ReservaSeleccionada?.Invoke(bar.ReservaId);
    }
}
