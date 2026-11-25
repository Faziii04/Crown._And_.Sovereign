using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class EstanciasControl : UserControl
{
    private readonly EstanciaService _service = new();
    public EstanciasControl()
    {
        InitializeComponent();
        FloorFilter.ItemsSource = new[] { "Todos los pisos", "Piso 1", "Piso 2", "Piso 3", "Piso 4", "Piso 5" };
        FloorFilter.SelectedIndex = 0;
        StatusFilter.ItemsSource = new[] { "Activas", "Historial" };
        StatusFilter.SelectedIndex = 0;
        StayDetail.CheckOutSolicitado += AbrirCheckOut;
        Recargar();
    }
    private void Recargar()
    {
        var floorName = FloorFilter.SelectedItem?.ToString() ?? "Todos los pisos";
        var floor = int.TryParse(floorName.Replace("Piso ", ""), out var parsed) ? parsed : (int?)null;
        StayGrid.ItemsSource = StatusFilter.SelectedIndex == 1 ? _service.Listar("Finalizada", SearchBox.Text, floor) : _service.ListarActivas(SearchBox.Text, floor);
    }
    private void Open(StayDirectoryEntry? row)
    {
        if (row is null) return;
        StayDetail.Mostrar(row.Id);
        WorkspaceTabs.SelectedIndex = 1;
    }
    public void AbrirHuesped(int guestId, bool abrirFolio = false)
    {
        Recargar();
        var activeRows = _service.ListarActivas();
        var row = activeRows.FirstOrDefault(item => item.GuestId == guestId || SampleData.ReservaHuespedes.Any(link => link.ReservaId == item.ReservationId && link.HuespedId == guestId));
        if (row is null) return;
        Open(row);
        if (abrirFolio) StayDetail.SeleccionarFolio();
    }
    private void Filter_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Floor_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Status_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Stay_DoubleClick(object sender, MouseButtonEventArgs e) => Open(StayGrid.SelectedItem as StayDirectoryEntry);
    private void OpenStay_Click(object sender, RoutedEventArgs e) => Open((sender as FrameworkElement)?.DataContext as StayDirectoryEntry);
    private void AbrirCheckOut(int stayId)
    {
        var dialog = new CheckOutDialog(stayId) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { Recargar(); WorkspaceTabs.SelectedIndex = 0; }
    }
}
