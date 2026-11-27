using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Gerencia.Politicas;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision.Politicas;

public partial class PoliticasAlojamientoControl : UserControl
{
    private readonly PoliticaHotelService _service = new();
    public PoliticasAlojamientoControl() { InitializeComponent(); Refresh(); }
    public event Action? ConfigurationChanged;
    public void Refresh()
    {
        CheckInValue.Text = _service.Valor("checkin"); CheckOutValue.Text = _service.Valor("checkout");
        EarlyValue.Text = _service.Valor("early"); LateValue.Text = _service.Valor("late"); PetsValue.Text = _service.Valor("pets");
    }
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string code) return;
        var definition = _service.Definiciones.First(x => x.Codigo == code);
        var dialog = new EditarPoliticaDialog(definition, _service.Valor(code)) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { Refresh(); ConfigurationChanged?.Invoke(); }
    }
}
