using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using System.Windows.Threading;

namespace Crown___Sovereign.Views.UserControls.Recepcion.Caja;

public partial class CorreccionesFinancierasControl : UserControl
{
    private readonly CorreccionFinancieraService _service = new();
    public CorreccionesFinancierasControl()
    {
        InitializeComponent(); StateBox.ItemsSource = new[] { "Todos", "Pendiente", "Autorizada", "Rechazada", "Ejecutada" }; StateBox.SelectedIndex = 0; Recargar();
    }
    public void Recargar()
    {
        if (StateBox is null) return;
        CorrectionsGrid.ItemsSource = _service.Listar(StateBox.SelectedItem?.ToString() ?? "Todos");
        var manager = SesionActual.Usuario?.Rol == "Gerente";
        foreach (var row in _service.Listar(StateBox.SelectedItem?.ToString() ?? "Todos")) { }
        if (CorrectionsGrid.Columns.LastOrDefault() is DataGridTemplateColumn actions && actions.CellTemplate is DataTemplate template)
        {
            _ = manager;
        }
    }
    public void MostrarCorreccion(string tipo, int id)
    {
        StateBox.SelectedItem = "Pendiente";
        Recargar();
        var row = _service.Listar("Pendiente").FirstOrDefault(item => item.Tipo == tipo && item.Id == id);
        if (row is null) return;
        CorrectionsGrid.SelectedItem = row;
        CorrectionsGrid.ScrollIntoView(row);
        CorrectionsGrid.Focus();
    }
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void CorrectionRow_Loading(object? sender, DataGridRowEventArgs e)
    {
        e.Row.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            var manager = SesionActual.Usuario?.Rol == "Gerente";
            var row = e.Row.Item as CorrectionDirectoryRow;
            foreach (var button in VisualButtons(e.Row))
            {
                var label = button.Content?.ToString();
                button.Visibility = label switch
                {
                    "Aprobar" or "Rechazar" => manager && row?.Estado == "Pendiente" ? Visibility.Visible : Visibility.Collapsed,
                    "Ejecutar" => !manager && row?.Estado == "Autorizada" ? Visibility.Visible : Visibility.Collapsed,
                    _ => Visibility.Collapsed
                };
            }
        });
    }
    private static IEnumerable<Button> VisualButtons(DependencyObject root)
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is Button button) yield return button;
            foreach (var nested in VisualButtons(child)) yield return nested;
        }
    }
    private void Approve_Click(object sender, RoutedEventArgs e) => Review(sender, true);
    private void Reject_Click(object sender, RoutedEventArgs e) => Review(sender, false);
    private void Review(object sender, bool approve)
    {
        if ((sender as FrameworkElement)?.Tag is not CorrectionDirectoryRow row) return;
        try
        {
            if (row.Tipo == "Devolución") { if (approve) _service.AutorizarDevolucion(row.Id); else _service.RechazarDevolucion(row.Id); }
            else { if (approve) _service.AutorizarAnulacion(row.Id); else _service.RechazarAnulacion(row.Id); }
            Recargar();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo revisar", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void Execute_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not CorrectionDirectoryRow row) return;
        try
        {
            if (row.Tipo == "Devolución") _service.EjecutarDevolucion(row.Id, ""); else _service.EjecutarAnulacion(row.Id, "");
            Recargar();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo ejecutar", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
