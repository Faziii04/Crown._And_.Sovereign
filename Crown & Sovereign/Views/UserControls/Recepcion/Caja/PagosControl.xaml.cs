using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Caja;

public partial class PagosControl : UserControl
{
    private readonly PagoService _service = new();
    public PagosControl()
    {
        InitializeComponent();
        ScopeBox.ItemsSource = new[] { "Turno actual", "Hoy", "Todos los pagos" }; ScopeBox.SelectedIndex = 0;
        MethodBox.ItemsSource = new[] { "Todos los medios" }.Concat(Data.SampleData.MetodosPago.Select(item => item.Nombre)).Distinct().ToList(); MethodBox.SelectedIndex = 0;
        StateBox.ItemsSource = new[] { "Todos los estados", "Aplicado", "Parcialmente devuelto", "Devuelto", "Anulado" }; StateBox.SelectedIndex = 0;
        Recargar();
    }
    public void Recargar()
    {
        var scope = ScopeBox.SelectedItem?.ToString() ?? "Turno actual";
        var rows = _service.Listar(SearchBox.Text, scope, MethodBox.SelectedItem?.ToString(), StateBox.SelectedItem?.ToString());
        PaymentsGrid.ItemsSource = rows;
        PaymentCountTitle.Text = $"Pagos · {scope.ToLowerInvariant()}";
        PaymentCountText.Text = rows.Count.ToString(); PaymentTotalText.Text = $"Bs {rows.Where(row => row.Estado != "Anulado").Sum(row => row.Monto):N2}";
        OutstandingText.Text = $"{_service.BuscarFolios("", "Todos", true).Count} folios";
    }
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void NewPayment_Click(object sender, RoutedEventArgs e)
    {
        var selector = new SelectorFolioPagoDialog { Owner = Window.GetWindow(this) };
        if (selector.ShowDialog() == true && selector.FolioIdSeleccionado is int id && new PagoDialog(id) { Owner = Window.GetWindow(this) }.ShowDialog() == true) Recargar();
    }
    private void Payment_DoubleClick(object sender, MouseButtonEventArgs e) => MostrarDetalle();
    private void Details_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is PaymentDirectoryRow row) MostrarDetalle(row);
    }
    private void MostrarDetalle(PaymentDirectoryRow? row = null)
    {
        row ??= PaymentsGrid.SelectedItem as PaymentDirectoryRow;
        if (row is null) return;
        new PagoDetalleDialog(row) { Owner = Window.GetWindow(this) }.ShowDialog(); Recargar();
    }
}
