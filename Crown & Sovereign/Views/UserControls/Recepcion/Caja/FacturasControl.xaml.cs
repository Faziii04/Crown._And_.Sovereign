using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Recepcion;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Caja;

public partial class FacturasControl : UserControl
{
    private readonly FacturaService _service = new();
    public FacturasControl()
    {
        InitializeComponent(); StateBox.ItemsSource = new[] { "Todos", "Emitida", "Anulada" }; StateBox.SelectedIndex = 0; Recargar();
    }
    public void Recargar() { if (InvoicesGrid is not null) InvoicesGrid.ItemsSource = _service.Listar(SearchBox?.Text ?? "", StateBox?.SelectedItem?.ToString() ?? "Todos"); }
    private void Filter_Changed(object sender, RoutedEventArgs e) => Recargar();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Preview_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is InvoiceDirectoryRow row) Mostrar(row); }
    private void Invoice_DoubleClick(object sender, MouseButtonEventArgs e) => Mostrar(InvoicesGrid.SelectedItem as InvoiceDirectoryRow);
    private void Mostrar(InvoiceDirectoryRow? row)
    {
        if (row is null) return;
        new FacturaPreviewDialog(row.Factura) { Owner = Window.GetWindow(this) }.ShowDialog();
    }
    private void NewInvoice_Click(object sender, RoutedEventArgs e)
    {
        var selector = new SelectorFolioPagoDialog(false) { Owner = Window.GetWindow(this) };
        if (selector.ShowDialog() != true || selector.FolioIdSeleccionado is not int folioId) return;
        try
        {
            var draft = _service.Preparar(folioId);
            var dialog = new FacturaEmitirDialog(draft) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true && dialog.FacturaEmitida is not null)
            {
                Recargar(); new FacturaPreviewDialog(dialog.FacturaEmitida) { Owner = Window.GetWindow(this) }.ShowDialog();
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se puede emitir", MessageBoxButton.OK, MessageBoxImage.Information); }
    }
}
