using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Tablero;

public partial class TableroHabitacionesControl : UserControl
{
    private readonly Dictionary<int, bool> _expanded = [];
    private IReadOnlyList<HabitacionOperacionModel> _rooms = [];
    private bool _detailed;
    public event Action<HabitacionOperacionModel>? DetalleSolicitado;
    public event Action<HabitacionOperacionModel, string>? AccionSolicitada;
    public event Action? LimpiarFiltrosSolicitado;
    public event Action<bool>? VistaCambiada;
    public TableroHabitacionesControl() => InitializeComponent();
    public bool VistaDetallada => _detailed;
    public double ScrollOffset { get => BoardScroll.VerticalOffset; set => BoardScroll.ScrollToVerticalOffset(value); }
    public void Mostrar(IReadOnlyList<HabitacionOperacionModel> rooms, int? piso)
    {
        foreach (var oldGroup in FloorList.Items.OfType<GrupoPisoOperativo>()) _expanded[oldGroup.Piso] = oldGroup.Expandido;
        _rooms = rooms;
        var selectedRooms = piso is int floor ? rooms.Where(item => item.Habitacion.Piso == floor).ToList() : rooms.ToList();
        var groups = selectedRooms.GroupBy(item => item.Habitacion.Piso).OrderBy(group => group.Key).Select(group => new GrupoPisoOperativo
        {
            Piso = group.Key, Titulo = $"Piso {group.Key}", Total = group.Count(),
            Resumen = $"{group.Count(item => item.Clasificacion == "Lista para ingreso")} listas · {group.Count(item => item.Clasificacion == "Ocupada")} ocupadas · {group.Count(item => item.Clasificacion == "Por preparar")} por preparar · {group.Count(item => item.Clasificacion == "Bloqueada")} bloqueadas",
            VistaDetallada = _detailed, Habitaciones = group.ToList(), Expandido = _expanded.GetValueOrDefault(group.Key, true)
        }).ToList();
        FloorList.ItemsSource = groups; CountText.Text = piso is null ? $"{selectedRooms.Count} resultado(s) · pisos 1–5" : $"{selectedRooms.Count} resultado(s) · Piso {piso}";
        EmptyState.Visibility = selectedRooms.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        FloorList.Visibility = selectedRooms.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }
    private void FloorSection_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not SeccionPisoControl section) return;
        section.DetalleSolicitado += room => DetalleSolicitado?.Invoke(room);
        section.AccionSolicitada += (room, action) => AccionSolicitada?.Invoke(room, action);
        if (section.DataContext is GrupoPisoOperativo group) group.Expandido = _expanded.GetValueOrDefault(group.Piso, group.Expandido);
    }
    private void Compact_Click(object sender, RoutedEventArgs e) { _detailed = false; DetailedButton.IsChecked = false; CompactButton.IsChecked = true; VistaCambiada?.Invoke(false); }
    private void Detailed_Click(object sender, RoutedEventArgs e) { _detailed = true; CompactButton.IsChecked = false; DetailedButton.IsChecked = true; VistaCambiada?.Invoke(true); }
    private void ClearFilters_Click(object sender, RoutedEventArgs e) => LimpiarFiltrosSolicitado?.Invoke();
}
