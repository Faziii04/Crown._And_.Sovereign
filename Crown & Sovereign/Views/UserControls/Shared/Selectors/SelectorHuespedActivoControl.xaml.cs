using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Shared.Selectors;

public partial class SelectorHuespedActivoControl : UserControl
{
    private readonly HuespedService _huespedes = new();
    public event Action<ActiveGuestChoice?>? HuespedSeleccionado;
    public ActiveGuestChoice? HuespedActual => GuestList.SelectedItem as ActiveGuestChoice;
    public SelectorHuespedActivoControl()
    {
        InitializeComponent(); FloorBox.ItemsSource = new[] { "Todos los pisos", "Piso 1", "Piso 2", "Piso 3", "Piso 4", "Piso 5" }; FloorBox.SelectedIndex = 0; Recargar();
    }
    private int? Piso => int.TryParse(FloorBox.SelectedItem?.ToString()?.Replace("Piso ", ""), out var floor) ? floor : null;
    private void Recargar()
    {
        var floor = Piso;
        RoomBox.ItemsSource = new[] { "Todas las habitaciones" }.Concat(_huespedes.ListarActivos().Where(item => floor is null || item.Floor == floor).Select(item => item.Room).Distinct().Order()).ToList();
        if (RoomBox.SelectedIndex < 0) RoomBox.SelectedIndex = 0;
        GuestList.ItemsSource = _huespedes.ListarActivos(SearchBox.Text, floor, RoomBox.SelectedItem?.ToString());
        if (GuestList.SelectedIndex < 0 && GuestList.Items.Count > 0) GuestList.SelectedIndex = 0;
        if (GuestList.SelectedItem is null) HuespedSeleccionado?.Invoke(null);
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Floor_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) { RoomBox.SelectedIndex = 0; Recargar(); } }
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Guest_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) HuespedSeleccionado?.Invoke(HuespedActual); }
    private void Select_Click(object sender, RoutedEventArgs e) { if (HuespedActual is { } guest) HuespedSeleccionado?.Invoke(guest); }
}
