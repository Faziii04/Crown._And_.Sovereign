using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Reservas.Wizard;

public sealed class ReservaHabitacionOpcion : INotifyPropertyChanged
{
    private bool _isSelected;
    public HabitacionModel Habitacion { get; }
    public string Numero => Habitacion.Numero;
    public string RoomDetails => $"{Habitacion.Tipo} · piso {Habitacion.Piso} · {Habitacion.Capacidad} personas · {Habitacion.Comodidades}";
    public decimal Total { get; }
    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected == value) return; _isSelected = value; OnPropertyChanged(); SelectionChanged?.Invoke(this, EventArgs.Empty); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? SelectionChanged;
    public ReservaHabitacionOpcion(HabitacionModel habitacion, decimal total, bool selected)
    { Habitacion = habitacion; Total = total; _isSelected = selected; }
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

public partial class PasoHabitacionesControl : UserControl
{
    private List<ReservaHabitacionOpcion> _options = [];
    public event Action? SeleccionCambiada;
    public IReadOnlyList<int> HabitacionesSeleccionadas => _options.Where(item => item.IsSelected).Select(item => item.Habitacion.Id).ToList();
    public int CapacidadSeleccionada => _options.Where(item => item.IsSelected).Sum(item => item.Habitacion.Capacidad);
    public IReadOnlyList<HabitacionModel> HabitacionesSeleccionadasModels => _options.Where(item => item.IsSelected).Select(item => item.Habitacion).ToList();
    public bool RequiereMismoPiso => SameFloorBox.IsChecked == true;
    public bool SeleccionEnMismoPiso => HabitacionesSeleccionadasModels.Select(item => item.Piso).Distinct().Count() <= 1;
    public TextBlock Asignacion => AssignmentSummary;

    public PasoHabitacionesControl()
    {
        InitializeComponent();
        FloorBox.ItemsSource = new[] { "Todos los pisos", "Piso 1", "Piso 2", "Piso 3", "Piso 4", "Piso 5" };
        TypeBox.ItemsSource = new[] { "Todos los tipos" }.Concat(SampleData.Tipos).ToList();
        CapacityBox.ItemsSource = new[] { "Cualquier capacidad", "2+ personas", "3+ personas", "4+ personas", "5+ personas", "6+ personas" };
        AmenityBox.ItemsSource = new[] { "Todas las comodidades", "Jacuzzi", "Balcón", "Minibar", "Wi-Fi", "TV", "Aire acondicionado" };
        FloorBox.SelectedIndex = TypeBox.SelectedIndex = CapacityBox.SelectedIndex = AmenityBox.SelectedIndex = 0;
    }

    public void Mostrar(IReadOnlyList<(HabitacionModel Habitacion, decimal Total)> candidates, IEnumerable<int> selectedIds)
    {
        var selected = selectedIds.ToHashSet();
        _options = candidates.Select(item => new ReservaHabitacionOpcion(item.Habitacion, item.Total, selected.Contains(item.Habitacion.Id))).ToList();
        foreach (var option in _options) option.SelectionChanged += RoomSelectionChanged;
        AplicarFiltros();
        ActualizarSeleccion();
    }

    public void EstablecerTipoPreferido(string? type)
    {
        if (!string.IsNullOrWhiteSpace(type) && TypeBox.Items.Contains(type)) TypeBox.SelectedItem = type;
    }

    public void MostrarCapacidad(int personas)
    {
        var capacity = CapacidadSeleccionada;
        var rooms = HabitacionesSeleccionadas;
        SelectionSummary.Text = $"{rooms.Count} habitación(es) marcada(s)";
        CapacitySummary.Text = $"{capacity} plazas para {personas} persona(s)";
        var sameFloorConflict = RequiereMismoPiso && !SeleccionEnMismoPiso;
        var enough = rooms.Count > 0 && capacity >= personas && !sameFloorConflict;
        CapacityNotice.Background = enough ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EAF5EC")) : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF4E5"));
        CapacityNotice.BorderBrush = enough ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9BC9A6")) : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E9BD78"));
        CapacityMessage.Text = rooms.Count == 0 ? "Selecciona al menos una habitación." : sameFloorConflict
            ? "La preferencia de mismo piso está activa. Quita las habitaciones de otros pisos o desactiva esa preferencia."
            : enough
            ? $"Capacidad suficiente: {capacity} plazas para {personas} persona(s)."
            : $"Capacidad insuficiente: faltan {personas - capacity} plaza(s). Añade otra habitación para alojar a todo el grupo.";
    }

    private void RoomSelectionChanged(object? sender, EventArgs e)
    {
        AplicarFiltros();
        ActualizarSeleccion();
        SeleccionCambiada?.Invoke();
    }
    private void ActualizarSeleccion()
    {
        var rooms = HabitacionesSeleccionadasModels;
        if (rooms.Count > 0 && SameFloorBox.IsChecked == true)
        {
            var floor = rooms[0].Piso;
            if (rooms.Any(room => room.Piso != floor))
                CapacityMessage.Text = "Las habitaciones marcadas están en pisos distintos. Quita las de otros pisos para mantenerlas juntas.";
        }
    }
    private void SearchChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) AplicarFiltros(); }
    private void FilterSelectionChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded) AplicarFiltros(); }
    private void PreferenceChanged(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        AplicarFiltros();
        ActualizarSeleccion();
        SeleccionCambiada?.Invoke();
    }

    private void AplicarFiltros()
    {
        if (RoomList is null || FloorBox is null) return;
        var query = SearchBox.Text.Trim();
        var floor = int.TryParse(FloorBox.SelectedItem?.ToString()?.Replace("Piso ", ""), out var parsedFloor) ? parsedFloor : (int?)null;
        var type = TypeBox.SelectedItem?.ToString();
        var minimumCapacity = int.TryParse(CapacityBox.SelectedItem?.ToString()?.Split(' ').FirstOrDefault(), out var capacity) ? capacity : (int?)null;
        var amenity = AmenityBox.SelectedItem?.ToString();
        var selectedFloor = _options.FirstOrDefault(item => item.IsSelected)?.Habitacion.Piso;
        RoomList.ItemsSource = _options.Where(option =>
            (string.IsNullOrWhiteSpace(query) || $"{option.Habitacion.Numero} {option.RoomDetails}".Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (floor is null || option.Habitacion.Piso == floor) &&
            (type is "Todos los tipos" || option.Habitacion.Tipo == type) &&
            (minimumCapacity is null || option.Habitacion.Capacidad >= minimumCapacity) &&
            (amenity is "Todas las comodidades" || option.Habitacion.Comodidades.Contains(amenity ?? "", StringComparison.OrdinalIgnoreCase)) &&
            (SameFloorBox.IsChecked != true || selectedFloor is null || option.Habitacion.Piso == selectedFloor || option.IsSelected)).ToList();
    }
}
