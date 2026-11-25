using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones;

public partial class FiltrosHabitacionesControl : UserControl
{
    public event Action? FiltrosCambiados;
    public FiltrosHabitacionesControl()
    {
        InitializeComponent();
        FloorBox.ItemsSource = new[] { "Todos los pisos", "Piso 1", "Piso 2", "Piso 3", "Piso 4", "Piso 5" };
        TypeBox.ItemsSource = new[] { "Todos los tipos" }.Concat(SampleData.Tipos).ToArray();
        OccupancyBox.ItemsSource = new[] { "Todas", "Disponible", "Ocupada" };
        CleaningBox.ItemsSource = new[] { "Todas" }.Concat(SampleData.Habitaciones.Select(r => r.Limpieza).Distinct()).ToArray();
        CapacityBox.ItemsSource = new[] { "Cualquier capacidad", "2 personas", "3 personas", "4 personas" };
        AmenityBox.ItemsSource = new[] { "Todas", "Jacuzzi", "Balcón", "Minibar", "Wi-Fi", "TV", "Aire acondicionado" };
        FloorBox.SelectedIndex = TypeBox.SelectedIndex = OccupancyBox.SelectedIndex = CleaningBox.SelectedIndex = CapacityBox.SelectedIndex = AmenityBox.SelectedIndex = 0;
    }
    public string Busqueda => SearchBox.Text;
    public int? Piso => int.TryParse(FloorBox.SelectedItem?.ToString()?.Replace("Piso ", ""), out var value) ? value : null;
    public string Tipo => TypeBox.SelectedItem?.ToString() ?? "Todos los tipos";
    public string Ocupacion => OccupancyBox.SelectedItem?.ToString() ?? "Todas";
    public string Limpieza => CleaningBox.SelectedItem?.ToString() ?? "Todas";
    public bool? Bloqueada => BlockedBox.IsChecked == true ? true : null;
    public int? CapacidadMinima => int.TryParse(CapacityBox.SelectedItem?.ToString()?.Split(' ').FirstOrDefault(), out var value) ? value : null;
    public string Comodidad => AmenityBox.SelectedItem?.ToString() ?? "Todas";
    private void SearchChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) FiltrosCambiados?.Invoke(); }
    private void SelectionChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded) FiltrosCambiados?.Invoke(); }
    private void CheckChanged(object sender, RoutedEventArgs e) { if (IsLoaded) FiltrosCambiados?.Invoke(); }
    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();FloorBox.SelectedIndex=TypeBox.SelectedIndex=OccupancyBox.SelectedIndex=CleaningBox.SelectedIndex=CapacityBox.SelectedIndex=AmenityBox.SelectedIndex=0;BlockedBox.IsChecked=false;
        FiltrosCambiados?.Invoke();
    }
}
