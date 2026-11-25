using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Estancias;

public partial class DetalleEstanciaControl : UserControl
{
    private int _stayId;
    public event Action<int>? CheckOutSolicitado;
    public DetalleEstanciaControl() => InitializeComponent();
    public void SeleccionarFolio() => WorkspaceTabs.SelectedIndex = 1;
    public void Mostrar(int stayId)
    {
        _stayId = stayId;
        var stay = new EstanciaService().ListarTodas().FirstOrDefault(item => item.Id == stayId);
        if (stay is null) return;
        GuestTitle.Text = stay.Guest;
        StaySubtitle.Text = $"Habitación {stay.Room} · Estancia #{stay.Id} · {stay.Package} · {stay.Status}";
        StayDates.Text = $"Llegada {stay.CheckIn:dd/MM/yyyy} · Salida prevista {stay.CheckOut:dd/MM/yyyy}";
        if (stay.Status == "Finalizada")
        {
            StayDates.Text += stay.CheckOutReal is DateTime checkout ? $" · Salida registrada {checkout:dd/MM/yyyy HH:mm}" : " · Histórica";
            CheckOutButton.Visibility = Visibility.Collapsed;
        }
        else CheckOutButton.Visibility = Visibility.Visible;
        FolioView.SetReadOnly(stay.Status == "Finalizada");
        KeysView.SetReadOnly(stay.Status == "Finalizada");
        SummaryView.Mostrar(stayId); FolioView.Mostrar(stayId); ServicesView.Mostrar(stayId); ConsumptionView.Mostrar(stayId); KeysView.Mostrar(stayId); ActivityView.Mostrar(stayId);
    }
    private void CheckOut_Click(object sender, RoutedEventArgs e) => CheckOutSolicitado?.Invoke(_stayId);
}
