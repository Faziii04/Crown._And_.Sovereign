using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;
namespace Crown___Sovereign.Views.UserControls.Operaciones.Inventario;
public partial class CatalogoProductosControl : UserControl
{
    private readonly InventarioService _service = new();
    public CatalogoProductosControl() { InitializeComponent(); Refresh(); }
    private void Refresh() => ProductsGrid.ItemsSource = _service.Buscar(SearchBox?.Text ?? "");
    private void Search_Changed(object sender, TextChangedEventArgs e) => Refresh();
    private void Create_Click(object sender, RoutedEventArgs e) => Edit(null);
    private void Edit_Click(object sender, RoutedEventArgs e) { if (ProductsGrid.SelectedItem is ProductoModel product) Edit(product); }
    private void Edit(ProductoModel? product) { var dialog = new ProductoDialog(product) { Owner = Window.GetWindow(this) }; if (dialog.ShowDialog() == true) Refresh(); }
}
