using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Views.UserControls.Shared.Cards;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones.Tablero;

public partial class SeccionPisoControl : UserControl
{
    public event Action<HabitacionOperacionModel>? DetalleSolicitado;
    public event Action<HabitacionOperacionModel, string>? AccionSolicitada;
    public SeccionPisoControl() => InitializeComponent();
    private bool Detailed => (DataContext as GrupoPisoOperativo)?.VistaDetallada == true;
    private void Compact_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not HabitacionCompactaCardControl card) return;
        card.Visibility = Detailed ? Visibility.Collapsed : Visibility.Visible;
        card.DetalleSolicitado += room => DetalleSolicitado?.Invoke(room); card.AccionSolicitada += (room, action) => AccionSolicitada?.Invoke(room, action);
    }
    private void Detailed_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not HabitacionDetalladaCardControl card) return;
        card.Visibility = Detailed ? Visibility.Visible : Visibility.Collapsed;
        card.DetalleSolicitado += room => DetalleSolicitado?.Invoke(room); card.AccionSolicitada += (room, action) => AccionSolicitada?.Invoke(room, action);
    }
}
