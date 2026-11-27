using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Gerencia.Politicas;

namespace Crown___Sovereign.Views.UserControls.Gerencia.Supervision.Politicas;

public partial class PoliticasPagosControl : UserControl
{
    private readonly PoliticaHotelService _service = new();
    public event Action? ConsultarFinanzas;
    public event Action? ConfigurationChanged;
    public PoliticasPagosControl() { InitializeComponent(); Refresh(); }
    private void Refresh() { AdvanceValue.Text = _service.Valor("advance"); GuaranteeValue.Text = _service.Valor("guarantee"); SettlementValue.Text = _service.Valor("settlement"); }
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string code) return;
        var definition = _service.Definiciones.First(x => x.Codigo == code);
        var dialog = new EditarPoliticaDialog(definition, _service.Valor(code)) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) { Refresh(); ConfigurationChanged?.Invoke(); }
    }
    private void Finance_Click(object sender, RoutedEventArgs e) => ConsultarFinanzas?.Invoke();
}
