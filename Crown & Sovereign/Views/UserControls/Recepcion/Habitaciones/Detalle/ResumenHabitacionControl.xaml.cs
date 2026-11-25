using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Detalle;

public partial class ResumenHabitacionControl : UserControl
{
    private HabitacionOperacionModel? _room;
    public event Action<HabitacionOperacionModel, string>? AccionSolicitada;
    public event Action<int>? EstanciaSolicitada;
    public event Action<int>? ReservaSolicitada;
    public ResumenHabitacionControl()
    {
        InitializeComponent(); CurrentStay.EstanciaSolicitada += id => EstanciaSolicitada?.Invoke(id); NextReservation.ReservaSolicitada += id => ReservaSolicitada?.Invoke(id);
    }
    public void Mostrar(HabitacionOperacionModel room)
    {
        _room = room; CurrentStay.Mostrar(room); NextReservation.Mostrar(room); TurnoverAlert.Visibility = room.CambioMismoDia ? Visibility.Visible : Visibility.Collapsed; MoveButton.Visibility = room.Bloqueada && room.Ocupada ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Action_Click(object sender, RoutedEventArgs e) { if (_room is not null && (sender as FrameworkElement)?.Tag is string action) AccionSolicitada?.Invoke(_room, action); }
    private void Preparation_Click(object sender, RoutedEventArgs e) { if (_room is not null) AccionSolicitada?.Invoke(_room, "Preparación"); }
}
