using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Listado;

public partial class ListadoReservasControl : UserControl
{
    private readonly ReservaService _reservas = new();
    private readonly ReservationListCriteria _criteria = new();
    private int _page = 1;
    public event Action? NuevaReservaSolicitada;
    public event Action<int>? ReservaSeleccionada;
    public event Action? DatosActualizados;
    public ListadoReservasControl()
    {
        InitializeComponent();
        StatusFilter.ItemsSource = new[] { "Todos los estados", "Confirmada", "Pendiente", "En estancia", "Finalizada", "Cancelada" }; StatusFilter.SelectedIndex = 0;
        ChannelFilter.ItemsSource = new[] { "Todos los canales" }.Concat(SampleData.Reservas.Select(item => item.Canal).Distinct()).ToList(); ChannelFilter.SelectedIndex = 0;
        TypeFilter.ItemsSource = new[] { "Todos los tipos" }.Concat(SampleData.Tipos).ToList(); TypeFilter.SelectedIndex = 0;
        FloorFilter.ItemsSource = new[] { "Todos los pisos", "Piso 1", "Piso 2", "Piso 3", "Piso 4", "Piso 5" }; FloorFilter.SelectedIndex = 0;
        Recargar();
    }
    public void Recargar()
    {
        _criteria.Busqueda = SearchBox?.Text ?? ""; _criteria.Estado = StatusFilter?.SelectedItem?.ToString() ?? "Todos los estados";
        _criteria.Canal = ChannelFilter?.SelectedItem?.ToString() ?? "Todos los canales"; _criteria.TipoHabitacion = TypeFilter?.SelectedItem?.ToString() ?? "Todos los tipos";
        _criteria.SoloGrupos = GroupsOnly?.IsChecked == true;
        _criteria.LlegadaDesde = ArrivalFrom?.SelectedDate; _criteria.LlegadaHasta = ArrivalTo?.SelectedDate;
        _criteria.SalidaDesde = DepartureFrom?.SelectedDate; _criteria.SalidaHasta = DepartureTo?.SelectedDate;
        var floorText = FloorFilter?.SelectedItem?.ToString() ?? "Todos los pisos";
        _criteria.Piso = int.TryParse(floorText.Replace("Piso ", ""), out var floor) ? floor : null;
        _criteria.Pagina = _page;
        var result = _reservas.ListarBandeja(_criteria); _page = result.Page;
        ReservationGrid.ItemsSource = result.Rows; CountLabel.Text = $"{result.Total} reservas · página {result.Page} de {Math.Max(1, (int)Math.Ceiling(result.Total / (double)result.PageSize))}";
        PageLabel.Text = $"{result.Page} / {Math.Max(1, (int)Math.Ceiling(result.Total / (double)result.PageSize))}";
        PreviousPage.IsEnabled = result.Page > 1; NextPage.IsEnabled = result.Page * result.PageSize < result.Total;
    }
    public void AplicarFiltroRapido(string filtro) { _criteria.FiltroRapido = filtro; _page = 1; Recargar(); }
    private void Filter_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) { _page = 1; Recargar(); } }
    private void Status_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) { _page = 1; Recargar(); } }
    private void Check_Changed(object sender, RoutedEventArgs e) { if (IsLoaded) { _page = 1; Recargar(); } }
    private void QuickFilter_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is string filter) AplicarFiltroRapido(filter); }
    private void Previous_Click(object sender, RoutedEventArgs e) { _page = Math.Max(1, _page - 1); Recargar(); }
    private void Next_Click(object sender, RoutedEventArgs e) { _page++; Recargar(); }
    private void NewReservation_Click(object sender, RoutedEventArgs e) => NuevaReservaSolicitada?.Invoke();
    private void Reservation_DoubleClick(object sender, MouseButtonEventArgs e) { if (ReservationGrid.SelectedItem is ReservationInboxRow row) ReservaSeleccionada?.Invoke(row.Id); }
    private void PrimaryAction_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not ReservationInboxRow row) return;
        if (row.PrimaryAction == "Check-in")
        {
            try { new EstanciaService().RealizarCheckIn(row.Id); Recargar(); DatosActualizados?.Invoke(); }
            catch (Exception ex) { MessageBox.Show(Window.GetWindow(this), ex.Message, "No se pudo realizar el check-in", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
        else if (row.PrimaryAction == "Confirmar")
        {
            try { new EstanciaService().ConfirmarReserva(row.Id); Recargar(); DatosActualizados?.Invoke(); }
            catch (Exception ex) { MessageBox.Show(Window.GetWindow(this), ex.Message, "No se pudo confirmar", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
        else if (row.PrimaryAction == "Check-out")
        {
            var guestId = SampleData.ReservaHuespedes.FirstOrDefault(link => link.ReservaId == row.Id && link.Principal)?.HuespedId
                ?? SampleData.Huespedes.FirstOrDefault(item => item.Nombre == row.Guest)?.Id;
            if (guestId is not int id) { MessageBox.Show(Window.GetWindow(this), "No se encontró el huésped principal de esta estancia.", "Check-out", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var dialog = new CheckOutDialog(id) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true) { Recargar(); DatosActualizados?.Invoke(); }
        }
        else ReservaSeleccionada?.Invoke(row.Id);
    }
}
