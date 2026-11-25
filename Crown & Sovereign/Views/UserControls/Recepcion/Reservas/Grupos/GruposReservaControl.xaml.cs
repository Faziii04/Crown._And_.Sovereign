using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Grupos;

public partial class GruposReservaControl : UserControl
{
    private readonly GrupoReservaService _service = new();
    public event Action<BorradorReserva>? BorradorReservaSolicitado;
    public GruposReservaControl() { InitializeComponent(); Recargar(); }
    public void Recargar() { GroupList.ItemsSource = _service.Listar(SearchBox?.Text ?? ""); }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Group_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (GroupList.SelectedItem is not GrupoReservaResumenModel summary) { SelectedName.Text = "Selecciona un grupo"; SelectedSummary.Text = ""; CreateReservationButton.IsEnabled = false; GuestsGrid.ItemsSource = null; return; }
        SelectedName.Text = summary.Grupo.Nombre; SelectedSummary.Text = $"{summary.Llegada:dd MMM yyyy} – {summary.Salida:dd MMM yyyy} · {summary.Estado}";
        GuestsCount.Text = summary.Huespedes.ToString(); RoomsCount.Text = summary.HabitacionesAsignadas.ToString(); ReservationsCount.Text = summary.ReservasRegistradas.ToString(); ArrivalsCount.Text = summary.LlegadasRegistradas.ToString();
        GuestsGrid.ItemsSource = _service.ConsultarHuespedes(summary.Grupo.Id); GuestsGrid.SelectedIndex = 0; CreateReservationButton.IsEnabled = true; AssignGuestButton.IsEnabled = GuestsGrid.Items.Count > 0; AssignRoomButton.IsEnabled = summary.ReservasRegistradas > 0;
    }
    private void CreateReservation_Click(object sender, RoutedEventArgs e)
    {
        if (GroupList.SelectedItem is not GrupoReservaResumenModel summary) return;
        var draft = new BorradorReserva { Llegada = summary.Llegada, Salida = summary.Salida, GrupoId = summary.Grupo.Id, PasoInicial = 2 };
        BorradorReservaSolicitado?.Invoke(draft);
    }
    private void AssignRoom_Click(object sender, RoutedEventArgs e)
    {
        if (GroupList.SelectedItem is not GrupoReservaResumenModel summary) return;
        var dialog = new AsignarHabitacionGrupoDialog(summary.Grupo) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { Recargar(); GroupList.SelectedItem = _service.Listar(SearchBox.Text).FirstOrDefault(item => item.Grupo.Id == summary.Grupo.Id); }
    }
    private void AssignGuest_Click(object sender, RoutedEventArgs e)
    {
        var guest = (sender as FrameworkElement)?.Tag as GrupoHuespedAsignacionModel ?? GuestsGrid.SelectedItem as GrupoHuespedAsignacionModel;
        if (guest is null) return;
        var dialog = new AsignarHuespedGrupoDialog(guest) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) Recargar();
    }
}
