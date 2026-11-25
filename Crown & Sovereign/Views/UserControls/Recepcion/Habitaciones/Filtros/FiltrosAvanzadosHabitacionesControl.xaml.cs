using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Filtros;

public partial class FiltrosAvanzadosHabitacionesControl : UserControl
{
    public event Action? FiltrosCambiados;
    public FiltrosAvanzadosHabitacionesControl()
    {
        InitializeComponent();
        OccupancyBox.ItemsSource = new[] { "Todas", "Ocupada", "Desocupada" }; OccupancyBox.SelectedIndex = 0;
        CleaningBox.ItemsSource = new[] { "Todos los estados", "Limpia", "Pendiente de limpieza", "En limpieza", "Pendiente de inspección", "Inspección fallida", "Sin verificar" }; CleaningBox.SelectedIndex = 0;
        MaintenanceBox.ItemsSource = new[] { "Todos los estados", "Operativa", "Con incidencia", "Bloqueada" }; MaintenanceBox.SelectedIndex = 0;
        CapacityBox.ItemsSource = new[] { "Cualquier capacidad", "2", "3", "4", "5", "6" }; CapacityBox.SelectedIndex = 0;
        FloorBox.ItemsSource = new[] { "Todos los pisos", "Piso 1", "Piso 2", "Piso 3", "Piso 4", "Piso 5" }; FloorBox.SelectedIndex = 0;
        ArrivalBox.ItemsSource = new[] { "Cualquiera", "Hoy", "Con reserva futura", "En riesgo" }; ArrivalBox.SelectedIndex = 0;
        DepartureBox.ItemsSource = new[] { "Cualquiera", "Hoy", "Con estancia activa" }; DepartureBox.SelectedIndex = 0;
        SortBox.ItemsSource = new[] { "Número de habitación", "Próxima llegada", "Próxima salida", "Prioridad operativa", "Tipo de habitación" }; SortBox.SelectedIndex = 0;
    }
    public FiltroHabitacionesOperativas Leer(string search, string type, string quick)
    {
        var floor = int.TryParse(FloorBox.SelectedItem?.ToString()?.Replace("Piso ", ""), out var parsedFloor) ? parsedFloor : (int?)null;
        return new FiltroHabitacionesOperativas { Busqueda = search, Tipo = type, FiltroRapido = ArrivalBox.SelectedItem?.ToString() == "En riesgo" ? "Llegadas en riesgo" : quick,
            Ocupacion = OccupancyBox.SelectedItem?.ToString() ?? "Todas", Limpieza = CleaningBox.SelectedItem?.ToString() ?? "Todos los estados",
            Mantenimiento = MaintenanceBox.SelectedItem?.ToString() ?? "Todos los estados", Llegada = ArrivalBox.SelectedItem?.ToString() ?? "Cualquiera", Salida = DepartureBox.SelectedItem?.ToString() ?? "Cualquiera", CapacidadMinima = int.TryParse(CapacityBox.SelectedItem?.ToString(), out var capacity) ? capacity : null,
            Piso = floor, Orden = SortBox.SelectedItem?.ToString() ?? "Número de habitación",
            Comodidades = new[] { WifiBox.IsChecked == true ? "Wi-Fi" : null, MinibarBox.IsChecked == true ? "Minibar" : null, JacuzziBox.IsChecked == true ? "Jacuzzi" : null, BalconyBox.IsChecked == true ? "Balcón" : null, TvBox.IsChecked == true ? "TV" : null, AirBox.IsChecked == true ? "Aire acondicionado" : null }.Where(item => item is not null).Cast<string>().ToList() };
    }
    public int? PisoSeleccionado => int.TryParse(FloorBox.SelectedItem?.ToString()?.Replace("Piso ", ""), out var floor) ? floor : null;
    public void SincronizarPiso(int? piso) => FloorBox.SelectedItem = piso is null ? "Todos los pisos" : $"Piso {piso}";
    public void Limpiar()
    {
        OccupancyBox.SelectedIndex = CleaningBox.SelectedIndex = MaintenanceBox.SelectedIndex = CapacityBox.SelectedIndex = FloorBox.SelectedIndex = ArrivalBox.SelectedIndex = DepartureBox.SelectedIndex = SortBox.SelectedIndex = 0;
        WifiBox.IsChecked = MinibarBox.IsChecked = JacuzziBox.IsChecked = BalconyBox.IsChecked = TvBox.IsChecked = AirBox.IsChecked = false;
        FiltrosCambiados?.Invoke();
    }
    private void FilterChanged(object sender, SelectionChangedEventArgs e) { ActualizarContador(); if (IsLoaded) FiltrosCambiados?.Invoke(); }
    private void FilterChecked(object sender, RoutedEventArgs e) { ActualizarContador(); if (IsLoaded) FiltrosCambiados?.Invoke(); }
    private void Clear_Click(object sender, RoutedEventArgs e) => Limpiar();
    private void ActualizarContador()
    {
        if (ActiveCountText is null) return;
        var count = (OccupancyBox?.SelectedIndex > 0 ? 1 : 0) + (CleaningBox?.SelectedIndex > 0 ? 1 : 0) + (MaintenanceBox?.SelectedIndex > 0 ? 1 : 0) + (CapacityBox?.SelectedIndex > 0 ? 1 : 0) + (FloorBox?.SelectedIndex > 0 ? 1 : 0) + (ArrivalBox?.SelectedIndex > 0 ? 1 : 0) + (DepartureBox?.SelectedIndex > 0 ? 1 : 0) + new[] { WifiBox?.IsChecked, MinibarBox?.IsChecked, JacuzziBox?.IsChecked, BalconyBox?.IsChecked, TvBox?.IsChecked, AirBox?.IsChecked }.Count(value => value == true);
        ActiveCountText.Text = $"{count} filtro(s) avanzado(s) activo(s).";
    }
}
