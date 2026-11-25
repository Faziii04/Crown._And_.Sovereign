using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Huespedes;

public partial class HuespedesControl : UserControl
{
    private readonly HuespedService _service = new();
    private int? _pendingGuestId;
    public event Action<string, int>? NavegacionSolicitada;
    public HuespedesControl()
    {
        InitializeComponent();
        DirectoryView.SeleccionCambiada += MostrarSeleccionado;
        DirectoryView.FiltroCambiado += RefreshSelection;
        SummaryView.FiltroSolicitado += AplicarFiltroResumen;
        ProfileView.EdicionSolicitada += EditGuest;
        ProfileView.VistaCompletaCambiada += CambiarModoExpandido;
        ProfileView.NavegacionSolicitada += (target, id) => NavegacionSolicitada?.Invoke(target, id);
        RefreshSelection();
    }
    public void SeleccionarHuesped(int id) { _pendingGuestId = id; DirectoryView.BuscarHuesped(id); RefreshSelection(); }
    private void RefreshSelection()
    {
        DirectoryView.Refrescar(_pendingGuestId);
        _pendingGuestId = null;
        var guest = DirectoryView.HuespedSeleccionado;
        if (guest is null) ProfileView.Limpiar(); else ProfileView.Mostrar(guest);
    }
    private void MostrarSeleccionado(HuespedModel? guest) { if (guest is null) ProfileView.Limpiar(); else ProfileView.Mostrar(guest); }
    private void AplicarFiltroResumen(string filter) { DirectoryView.AplicarFiltroResumen(filter); RefreshSelection(); }
    private void CambiarModoExpandido(bool expanded)
    {
        DirectoryColumn.Width = expanded ? new GridLength(0) : new GridLength(320);
        DirectorySplitter.Visibility = expanded ? Visibility.Collapsed : Visibility.Visible;
        DirectoryView.Visibility = expanded ? Visibility.Collapsed : Visibility.Visible;
        ProfileBorder.Margin = expanded ? new Thickness(0) : new Thickness(10,0,0,0);
    }
    private void NewGuest_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new HuespedDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true && dialog.HuespedGuardado is int id) { SummaryView.Actualizar(); SeleccionarHuesped(id); }
    }
    private void EditGuest(HuespedModel guest)
    {
        var dialog = new HuespedDialog(guest) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { _pendingGuestId = guest.Id; RefreshSelection(); }
    }
}
