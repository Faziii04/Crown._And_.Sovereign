using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Shared.Cards;

public partial class HabitacionCardControl : UserControl
{
    public event Action<HabitacionModel>? RoomSelected;
    public HabitacionCardControl() { InitializeComponent(); Loaded += (_, _) => BlockedLabel.Visibility = (DataContext as HabitacionModel)?.Bloqueada == true ? Visibility.Visible : Visibility.Collapsed; }
    private void Details_Click(object sender, RoutedEventArgs e) { if (DataContext is HabitacionModel room) RoomSelected?.Invoke(room); }
}
