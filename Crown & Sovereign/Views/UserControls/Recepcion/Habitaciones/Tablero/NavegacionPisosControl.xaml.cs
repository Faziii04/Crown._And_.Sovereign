using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Tablero;

public partial class NavegacionPisosControl : UserControl
{
    private int? _selected;
    public event Action<int?>? PisoSeleccionado;
    public NavegacionPisosControl() => InitializeComponent();
    public void Mostrar(IReadOnlyList<HabitacionOperacionModel> rooms, int? selected)
    {
        _selected = selected;
        var options = new List<OpcionPisoHabitaciones> { new(null, "Todos los pisos", rooms.Count, rooms.Count(item => item.Clasificacion == "Lista para ingreso"), selected is null) };
        options.AddRange(rooms.GroupBy(item => item.Habitacion.Piso).OrderBy(group => group.Key).Select(group => new OpcionPisoHabitaciones(group.Key, $"Piso {group.Key}", group.Count(), group.Count(item => item.Clasificacion == "Lista para ingreso"), selected == group.Key)));
        FloorOptions.ItemsSource = options;
    }
    private void Floor_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is int floor) PisoSeleccionado?.Invoke(_selected == floor ? null : floor);
        else PisoSeleccionado?.Invoke(null);
    }
}
