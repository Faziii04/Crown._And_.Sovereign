using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Filtros;

public partial class FiltrosHabitacionesControl : UserControl
{
    private readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private string _quick = "Todas";
    private int? _floor;
    public event Action? FiltrosCambiados;
    public FiltrosHabitacionesControl()
    {
        InitializeComponent();
        TypeBox.ItemsSource = new[] { "Todos los tipos" }.Concat(SampleData.Tipos).ToList(); TypeBox.SelectedIndex = 0;
        SortBox.ItemsSource = new[] { "Número de habitación", "Próxima llegada", "Próxima salida", "Prioridad operativa", "Tipo de habitación" }; SortBox.SelectedIndex = 0;
        _debounce.Tick += (_, _) => { _debounce.Stop(); FiltrosCambiados?.Invoke(); };
    }
    public FiltroHabitacionesOperativas Filtro => ConPiso(_floor);
    public int? Piso => _floor;
    public void SeleccionarPiso(int? floor) { _floor = floor; AdvancedFilters.SincronizarPiso(floor); Actualizar(); }
    public void SeleccionarFiltroRapido(string filter) { _quick = _quick == filter ? "Todas" : filter; ActualizarEstiloRapido(); Actualizar(); }
    public void MostrarCantidad(int count) => ResultText.Text = $"{count} habitación(es) coinciden con los filtros.";
    private void SearchChanged(object sender, TextChangedEventArgs e) { _debounce.Stop(); _debounce.Start(); }
    private void FilterChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Actualizar(); }
    private void Advanced_Changed() { _floor = AdvancedFilters.PisoSeleccionado; if (IsLoaded) Actualizar(); }
    private void Quick_Click(object sender, RoutedEventArgs e)
    {
        var filter = (sender as FrameworkElement)?.Tag?.ToString() ?? "Todas";
        _quick = filter == _quick || filter == "Todas" ? "Todas" : filter;
        ActualizarEstiloRapido(); Actualizar();
    }
    private void ActualizarEstiloRapido()
    {
        foreach (var button in QuickFilters.Children.OfType<Button>())
        {
            var selected = button.Tag?.ToString() == _quick;
            button.Background = selected ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E8EEF3")) : System.Windows.Media.Brushes.White;
            button.BorderBrush = selected ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3E5974")) : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#DCE2E8"));
        }
    }
    private void Actualizar() { FiltrosCambiados?.Invoke(); }
    private FiltroHabitacionesOperativas ConPiso(int? floor) { var filter = AdvancedFilters.Leer(SearchBox.Text, TypeBox.SelectedItem?.ToString() ?? "Todos los tipos", _quick); filter.Piso = floor; filter.Orden = SortBox.SelectedItem?.ToString() ?? filter.Orden; return filter; }
}
