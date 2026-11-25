using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;
namespace Crown___Sovereign.Views.UserControls.Operaciones.Inventario;
public partial class ExistenciasControl : UserControl
{
    private readonly InventarioService _service = new();
    public ExistenciasControl() { InitializeComponent(); Refresh(); }
    private void Refresh() => StockGrid.ItemsSource = _service.Buscar().Select(product => new { product.Nombre, product.Categoria, product.Stock, product.StockMinimo, EstadoStock = product.Stock <= product.StockMinimo ? "Reponer" : "Disponible" }).ToList();
    private void Movement_Click(object sender, RoutedEventArgs e) { var dialog = new MovimientoInventarioDialog { Owner = Window.GetWindow(this) }; if (dialog.ShowDialog() == true) Refresh(); }
}
