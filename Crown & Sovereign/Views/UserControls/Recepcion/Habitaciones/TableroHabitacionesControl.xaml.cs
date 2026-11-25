using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Views.UserControls.Shared.Cards;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones;

public partial class TableroHabitacionesControl : UserControl
{
    public event Action<HabitacionModel>? HabitacionSeleccionada;
    public TableroHabitacionesControl() => InitializeComponent();
    public void Mostrar(IReadOnlyList<HabitacionModel> rooms)
    {
        FloorList.ItemsSource = rooms.GroupBy(room => room.Piso).OrderBy(group => group.Key).Select(group => new RoomFloorGroup($"Piso {group.Key}", group.Count(), group.ToList())).ToList();
    }
    private void RoomCard_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is HabitacionCardControl card) card.RoomSelected += room => HabitacionSeleccionada?.Invoke(room);
    }
}
