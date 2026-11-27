using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Gerencia.Politicas;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision.Politicas;

public partial class PoliticasReservasControl : UserControl
{
    private readonly PoliticaHotelService _service = new();
    public PoliticasReservasControl() { InitializeComponent(); Refresh(); }
    public event Action? ConfigurationChanged;
    public void Refresh() { FreeCancelValue.Text = _service.Valor("reservation_cancel_hours"); LateCancelValue.Text = _service.Valor("reservation_cancel_conditions"); NoShowValue.Text = _service.Valor("reservation_noshow"); }
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string code) return;
        var definition = _service.Definiciones.First(x => x.Codigo == code);
        var dialog = new EditarPoliticaDialog(definition, _service.Valor(code)) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { Refresh(); ConfigurationChanged?.Invoke(); }
    }
    private void Simulate_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SimularCancelacionReservaDialog(_service) { Owner = Window.GetWindow(this) };
        dialog.ShowDialog();
    }
}
