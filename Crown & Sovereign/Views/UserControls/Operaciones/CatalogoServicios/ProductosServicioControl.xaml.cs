using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Operaciones.CatalogoServicios;

public partial class ProductosServicioControl : UserControl
{
    private readonly CatalogoServiciosService _service = new();
    private ServicioModel? _current;
    public event EventHandler? ConfigurationChanged;
    public ProductosServicioControl() => InitializeComponent();
    public void Mostrar(ServicioModel service)
    {
        _current = service;
        ProductGrid.ItemsSource = _service.ProductosDe(service.Id).Where(item => item.Producto is not null).Select(item => new ServicioProductoFila(service.Id, item.Relacion.ProductoId, item.Producto!.Nombre, item.Producto.Categoria, item.Relacion.CantidadIncluida, item.Producto.Stock)).ToList();
        ProductBox.ItemsSource = SampleData.Productos.OrderBy(item => item.Categoria).ThenBy(item => item.Nombre).ToList();
    }
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null || ProductBox.SelectedItem is not ProductoModel product) return;
        if (!int.TryParse(QuantityBox.Text, out var quantity)) { MessageBox.Show("Indica una cantidad entera mayor que cero.", "Cantidad inválida", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        try { _service.AsociarProducto(_current.Id, product.Id, quantity); Mostrar(_current); ConfigurationChanged?.Invoke(this, EventArgs.Empty); } catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo asociar", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null || ProductGrid.SelectedItem is not ServicioProductoFila row) return;
        _service.QuitarProducto(_current.Id, row.ProductoId); Mostrar(_current); ConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }
}
